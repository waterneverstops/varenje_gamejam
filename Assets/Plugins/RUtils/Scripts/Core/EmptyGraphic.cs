using UnityEngine.UI;

namespace Plugins.RProjects.RUtils.Scripts.Core
{
    public class EmptyGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}