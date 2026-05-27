using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Plugins.RProjects.RUtils.Scripts.Editor
{
    public class MissingScriptWindow : EditorWindow
    {
        private static readonly List<string> MissingAssets = new();
        private static readonly List<GameObject> ObjectsWithMissingScriptsInCurrentScene = new();

        [MenuItem("Escape/Find missing scripts")]
        public static void ShowWindow()
        {
            GetWindow(typeof(MissingScriptWindow));
        }

        private void OnGUI()
        {
            GUILayout.Label("Find Missing Scripts", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUIStyle myBoxStyle = new GUIStyle(GUI.skin.box);
            myBoxStyle.normal.background = MakeTex(2, 2, new Color(0.6f, 0.6f, 0.6f, 0.5f));

            // Scene block
            GUILayout.BeginVertical(myBoxStyle);
            if (GUILayout.Button("Find in current scene"))
            {
                FindMissingScriptsInCurrentScene();
            }
            GUILayout.Label("Results (Current Scene):", EditorStyles.boldLabel);
            foreach (var go in ObjectsWithMissingScriptsInCurrentScene)
            {
                if (GUILayout.Button(go.name))
                {
                    EditorGUIUtility.PingObject(go);
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(20);

            // Assets block
            GUILayout.BeginVertical(myBoxStyle);
            if (GUILayout.Button("Find in assets"))
            {
                FindMissingScriptsInAssets();
            }
            GUILayout.Label("Results (Assets):", EditorStyles.boldLabel);
            foreach (string path in MissingAssets)
            {
                if (GUILayout.Button(path))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
                }
            }
            GUILayout.EndVertical();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private static void FindMissingScriptsInCurrentScene()
        {
            ObjectsWithMissingScriptsInCurrentScene.Clear();
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go.transform.parent == null) // Only start with root objects
                {
                    FindMissingScriptsInGameObjectAndChildren(go);
                }
            }
        }

        private static void FindMissingScriptsInGameObjectAndChildren(GameObject go)
        {
            var components = go.GetComponents<Component>();
            bool hasMissingScript = components.Any(c => c == null);
            if (hasMissingScript)
            {
                ObjectsWithMissingScriptsInCurrentScene.Add(go);
            }
            foreach (Transform child in go.transform) // Recursively check children
            {
                FindMissingScriptsInGameObjectAndChildren(child.gameObject);
            }
        }

        private static void FindMissingScriptsInAssets()
        {
            MissingAssets.Clear();
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            foreach (string assetPath in allAssets)
            {
                if (Path.GetExtension(assetPath) == ".prefab")
                {
                    var assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    var components = assetRoot.GetComponentsInChildren<Component>(true);
                    bool hasMissingScript = components.Any(c => c == null);
                    if (hasMissingScript)
                    {
                        MissingAssets.Add(assetPath);
                    }
                }
            }
        }
    }
}
