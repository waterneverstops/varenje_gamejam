using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace Plugins.RProjects.RUtils.Scripts.Editor
{
    public class CodeStatisticsWindow : EditorWindow
    {
        private const string ScriptsRootPath = "Assets/Scripts";

        private CodeStatisticsSnapshot _snapshot;
        private Vector2 _scrollPosition;
        private bool[] _groupFoldouts;

        [MenuItem("Tools/Code Statistics")]
        public static void ShowWindow()
        {
            var window = GetWindow<CodeStatisticsWindow>();
            window.titleContent = new GUIContent("Code Stats");
            window.minSize = new Vector2(320f, 220f);
            window.RefreshStatistics();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshStatistics();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Scripts Folder Statistics", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
                {
                    RefreshStatistics();
                }
            }

            EditorGUILayout.Space();

            if (_snapshot == null)
            {
                EditorGUILayout.HelpBox("Statistics are not available.", MessageType.Info);
                return;
            }

            if (!string.IsNullOrEmpty(_snapshot.ErrorMessage))
            {
                EditorGUILayout.HelpBox(_snapshot.ErrorMessage, MessageType.Error);
                return;
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollView.scrollPosition;

                DrawMetric("Scripts path", _snapshot.RootPath);
                DrawMetric("Total script files", _snapshot.ScriptFilesCount.ToString());
                DrawMetric("Total lines", _snapshot.TotalLinesCount.ToString());
                DrawMetric("Average lines per file", _snapshot.AverageLinesPerFile.ToString("F2"));
                DrawMetric("Last updated", _snapshot.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"));

                EditorGUILayout.Space(12f);
                GUILayout.Label("Files By Line Count Groups", EditorStyles.boldLabel);
                DrawChart(_snapshot);
                DrawGroupsList(_snapshot);
            }
        }

        private static void DrawMetric(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(150f));
                EditorGUILayout.LabelField(value);
            }
        }

        private void RefreshStatistics()
        {
            _snapshot = BuildSnapshot();
            _groupFoldouts = _snapshot?.Buckets != null ? new bool[_snapshot.Buckets.Length] : Array.Empty<bool>();
            Repaint();
        }

        private static CodeStatisticsSnapshot BuildSnapshot()
        {
            var absoluteScriptsPath = Path.Combine(Directory.GetCurrentDirectory(), ScriptsRootPath);

            if (!Directory.Exists(absoluteScriptsPath))
            {
                return CodeStatisticsSnapshot.WithError($"Folder not found: {ScriptsRootPath}");
            }

            var scriptPaths = Directory
                .EnumerateFiles(absoluteScriptsPath, "*.cs", SearchOption.AllDirectories)
                .ToArray();

            var totalLines = scriptPaths.Sum(CountLinesInFile);
            var averageLines = scriptPaths.Length > 0 ? (float)totalLines / scriptPaths.Length : 0f;

            return new CodeStatisticsSnapshot
            {
                RootPath = ScriptsRootPath,
                ScriptFilesCount = scriptPaths.Length,
                TotalLinesCount = totalLines,
                AverageLinesPerFile = averageLines,
                GeneratedAt = DateTime.Now,
                Buckets = BuildBuckets(scriptPaths)
            };
        }

        private static CodeStatisticsBucket[] BuildBuckets(string[] scriptPaths)
        {
            if (scriptPaths.Length == 0)
            {
                return Array.Empty<CodeStatisticsBucket>();
            }

            var ranges = new[]
            {
                new CodeStatisticsRange(0, 15),
                new CodeStatisticsRange(16, 50),
                new CodeStatisticsRange(51, 100),
                new CodeStatisticsRange(101, 200),
                new CodeStatisticsRange(201, 400),
                new CodeStatisticsRange(401, int.MaxValue)
            };

            var fileLines = scriptPaths
                .Select(path => new FileLinesInfo
                {
                    RelativePath = GetRelativeAssetPath(path),
                    LinesCount = CountLinesInFile(path)
                })
                .ToArray();

            var buckets = ranges
                .Select(range => new CodeStatisticsBucket
                {
                    Label = range.MaxLines == int.MaxValue
                        ? $"{range.MinLines}+"
                        : $"{range.MinLines}-{range.MaxLines}",
                    Files = fileLines
                        .Where(file => file.LinesCount >= range.MinLines && file.LinesCount <= range.MaxLines)
                        .OrderBy(file => file.RelativePath)
                        .ToArray()
                })
                .Select(bucket =>
                {
                    bucket.FilesCount = bucket.Files.Length;
                    return bucket;
                })
                .ToArray();

            return buckets;
        }

        private static void DrawChart(CodeStatisticsSnapshot snapshot)
        {
            if (snapshot.Buckets == null || snapshot.Buckets.Length == 0)
            {
                EditorGUILayout.HelpBox("No script files found for the chart.", MessageType.Info);
                return;
            }

            var chartRect = GUILayoutUtility.GetRect(10f, 220f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(chartRect, new Color(0.16f, 0.16f, 0.16f));

            var maxFilesCount = Mathf.Max(1, snapshot.Buckets.Max(bucket => bucket.FilesCount));
            const float chartPadding = 8f;
            const float bottomLabelHeight = 22f;
            const float topLabelHeight = 18f;
            const float topInfoHeight = 18f;
            const float columnGap = 6f;

            var innerRect = new Rect(
                chartRect.x + chartPadding,
                chartRect.y + topInfoHeight + 4f,
                Mathf.Max(0f, chartRect.width - chartPadding * 2f),
                Mathf.Max(0f, chartRect.height - topInfoHeight - bottomLabelHeight - topLabelHeight - 12f));

            var totalGapWidth = columnGap * Mathf.Max(0, snapshot.Buckets.Length - 1);
            var columnWidth = snapshot.Buckets.Length > 0
                ? Mathf.Max(8f, (innerRect.width - totalGapWidth) / snapshot.Buckets.Length)
                : innerRect.width;

            for (var i = 0; i < snapshot.Buckets.Length; i++)
            {
                var bucket = snapshot.Buckets[i];
                var normalizedHeight = (float)bucket.FilesCount / maxFilesCount;
                var barHeight = innerRect.height * normalizedHeight;
                var x = innerRect.x + (columnWidth + columnGap) * i;
                var y = innerRect.yMax - barHeight;
                var barRect = new Rect(x, y, columnWidth, barHeight);

                EditorGUI.DrawRect(barRect, new Color(0.3f, 0.7f, 0.95f));

                var filesLabelRect = new Rect(x, Mathf.Max(chartRect.y + topInfoHeight, y - topLabelHeight), columnWidth, topLabelHeight);
                GUI.Label(filesLabelRect, bucket.FilesCount.ToString(), EditorStyles.centeredGreyMiniLabel);

                var rangeLabelRect = new Rect(x, chartRect.yMax - bottomLabelHeight, columnWidth, bottomLabelHeight);
                GUI.Label(rangeLabelRect, bucket.Label, EditorStyles.centeredGreyMiniLabel);
            }

            GUI.Label(
                new Rect(chartRect.x + chartPadding, chartRect.y + 4f, chartRect.width - chartPadding * 2f, topInfoHeight),
                "Top labels: files in range. Bottom labels: line count ranges.",
                EditorStyles.miniLabel);
        }

        private void DrawGroupsList(CodeStatisticsSnapshot snapshot)
        {
            if (snapshot.Buckets == null || snapshot.Buckets.Length == 0)
            {
                return;
            }

            EditorGUILayout.Space(12f);
            GUILayout.Label("Scripts In Groups", EditorStyles.boldLabel);

            for (var i = 0; i < snapshot.Buckets.Length; i++)
            {
                var bucket = snapshot.Buckets[i];
                var header = $"{bucket.Label} ({bucket.FilesCount})";
                _groupFoldouts[i] = EditorGUILayout.Foldout(_groupFoldouts[i], header, true);

                if (!_groupFoldouts[i])
                {
                    continue;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    if (bucket.FilesCount == 0)
                    {
                        EditorGUILayout.LabelField("No scripts in this group.");
                        continue;
                    }

                    foreach (var file in bucket.Files)
                    {
                        EditorGUILayout.LabelField($"{file.RelativePath} ({file.LinesCount})");
                    }
                }
            }
        }

        private static string GetRelativeAssetPath(string absolutePath)
        {
            var normalizedPath = absolutePath.Replace('\\', '/');
            var projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');

            if (!normalizedPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath;
            }

            var relativePath = normalizedPath.Substring(projectRoot.Length).TrimStart('/');
            return relativePath;
        }

        private static int CountLinesInFile(string filePath)
        {
            var linesCount = 0;

            using (var reader = new StreamReader(filePath))
            {
                while (reader.ReadLine() != null)
                {
                    linesCount++;
                }
            }

            return linesCount;
        }

        private class CodeStatisticsSnapshot
        {
            public string RootPath;
            public int ScriptFilesCount;
            public int TotalLinesCount;
            public float AverageLinesPerFile;
            public DateTime GeneratedAt;
            public string ErrorMessage;
            public CodeStatisticsBucket[] Buckets;

            public static CodeStatisticsSnapshot WithError(string errorMessage)
            {
                return new CodeStatisticsSnapshot
                {
                    RootPath = ScriptsRootPath,
                    GeneratedAt = DateTime.Now,
                    ErrorMessage = errorMessage
                };
            }
        }

        private class FileLinesInfo
        {
            public string RelativePath;
            public int LinesCount;
        }

        private class CodeStatisticsBucket
        {
            public string Label;
            public int FilesCount;
            public FileLinesInfo[] Files;
        }

        private readonly struct CodeStatisticsRange
        {
            public CodeStatisticsRange(int minLines, int maxLines)
            {
                MinLines = minLines;
                MaxLines = maxLines;
            }

            public int MinLines { get; }
            public int MaxLines { get; }
        }
    }

    [InitializeOnLoad]
    public static class CodeStatisticsToolbar
    {
        static CodeStatisticsToolbar()
        {
            ToolbarExtender.RightToolbarGUI.Add(DrawToolbarButton);
        }

        private static void DrawToolbarButton()
        {
            if (GUILayout.Button("Code Stats", GUILayout.Width(90f)))
            {
                CodeStatisticsWindow.ShowWindow();
            }
        }
    }
}
