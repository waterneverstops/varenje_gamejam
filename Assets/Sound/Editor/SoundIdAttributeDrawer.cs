using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SoundIdAttribute))]
public sealed class SoundIdAttributeDrawer : PropertyDrawer
{
    private const string EmptyOption = "<None>";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.HelpBox(position, "[SoundId] works only with string fields.", MessageType.Warning);
            return;
        }

        SoundIdAttribute soundIdAttribute = (SoundIdAttribute)attribute;
        List<string> ids = CollectIds(soundIdAttribute);
        if (ids.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        string currentValue = property.stringValue;
        GUIContent[] options = BuildOptions(ids, currentValue, out int currentIndex);
        int selectedIndex = EditorGUI.Popup(position, label, currentIndex, options);

        if (selectedIndex == 0)
        {
            property.stringValue = string.Empty;
        }
        else if (selectedIndex <= ids.Count)
        {
            property.stringValue = ids[selectedIndex - 1];
        }
    }

    private static List<string> CollectIds(SoundIdAttribute soundIdAttribute)
    {
        string[] guids = AssetDatabase.FindAssets("t:SoundLibrary");
        HashSet<string> uniqueIds = new HashSet<string>();
        List<string> ids = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SoundLibrary library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(path);
            if (library == null)
            {
                continue;
            }

            IReadOnlyList<SoundEntry> entries = library.Entries;
            for (int j = 0; j < entries.Count; j++)
            {
                SoundEntry entry = entries[j];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                {
                    continue;
                }

                if (soundIdAttribute.FilterByType && entry.Type != soundIdAttribute.Type)
                {
                    continue;
                }

                if (uniqueIds.Add(entry.Id))
                {
                    ids.Add(entry.Id);
                }
            }
        }

        ids.Sort(StringComparer.OrdinalIgnoreCase);
        return ids;
    }

    private static GUIContent[] BuildOptions(IReadOnlyList<string> ids, string currentValue, out int currentIndex)
    {
        List<GUIContent> options = new List<GUIContent> {
            new GUIContent(EmptyOption)
        };

        currentIndex = 0;
        for (int i = 0; i < ids.Count; i++)
        {
            options.Add(new GUIContent(ids[i]));
            if (ids[i] == currentValue)
            {
                currentIndex = i + 1;
            }
        }

        if (!string.IsNullOrEmpty(currentValue) && currentIndex == 0)
        {
            currentIndex = options.Count;
            options.Add(new GUIContent($"Missing: {currentValue}"));
        }

        return options.ToArray();
    }
}
