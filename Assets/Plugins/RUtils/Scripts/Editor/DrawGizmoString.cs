using UnityEngine;

namespace Plugins.RProjects.RUtils.Scripts.Editor
{
    public static class DrawGizmoString
    {
        public static void DrawString(string text, Vector3 worldPos, bool onlySceneCamera = false, float oX = 0, float oY = 0, Color? colour = null)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.BeginGUI();

            var restoreColor = GUI.color;

            if (colour.HasValue) GUI.color = colour.Value;
            var screenPos = GetCamera(onlySceneCamera).WorldToScreenPoint(worldPos);

            if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                GUI.color = restoreColor;
                UnityEditor.Handles.EndGUI();
                return;
            }

            var position = TransformByPixel(worldPos, oX, oY, onlySceneCamera);
            UnityEditor.Handles.Label(position, text);

            GUI.color = restoreColor;
            UnityEditor.Handles.EndGUI();
#endif
        }

        static Vector3 TransformByPixel(Vector3 position, float x, float y, bool isSceneCamera)
        {
            return TransformByPixel(position, new Vector3(x, y), isSceneCamera);
        }

        static Vector3 TransformByPixel(Vector3 position, Vector3 translateBy, bool isSceneCamera)
        {
            var cam = GetCamera(isSceneCamera);
            return cam ? cam.ScreenToWorldPoint(cam.WorldToScreenPoint(position) + translateBy) : position;
        }

        static Camera GetCamera(bool isScene)
        {
            return isScene ? UnityEditor.SceneView.currentDrawingSceneView.camera : Camera.main;
        }
    }
}