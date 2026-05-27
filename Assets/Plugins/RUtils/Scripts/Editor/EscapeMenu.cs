using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plugins.RProjects.RUtils.Scripts.Editor
{
    public static class StateMenuItems {
        [MenuItem("R/State/OpenDirectory")]
        public static void OpenDirectory() {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("R/State/CleanUpCompletely")]
        public static void CleanUpCompletely() {
            CleanUpPlayerPrefs();
            CleanUpFiles();
        }

        [MenuItem("R/State/CleanUpPlayerPrefs")]
        public static void CleanUpPlayerPrefs() {
            Debug.Log("Clean up player prefs");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        [MenuItem("R/State/CleanUpFiles")]
        public static void CleanUpFiles() {
            var targetPath = Application.persistentDataPath;
            Debug.Log($"Clean up '{targetPath}'");
            var files = Directory.GetFiles(targetPath);
            foreach ( var file in files ) {
                File.Delete(file);
            }
            var directories = Directory.GetDirectories(targetPath);
            foreach ( var directory in directories ) {
                Directory.Delete(directory, true);
            }
        }
    }
}
