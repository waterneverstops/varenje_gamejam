using UnityEngine;

namespace Plugins.RProjects.RUtils.Scripts.Core
{
    public class EditorOnly : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isPlaying)
            {
                gameObject.SetActive(false);
            }
        }
    }
}