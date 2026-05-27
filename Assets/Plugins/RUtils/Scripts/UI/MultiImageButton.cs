using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.RProjects.RUtils.Scripts.UI
{
    public class MultiImageButton : Button
    {
        private Graphic[] _mGraphics;

        private IEnumerable<Graphic> Graphics
        {
            get
            {
                if (_mGraphics == null && targetGraphic)
                {
                    _mGraphics = targetGraphic.transform.GetComponentsInChildren<Graphic>();
                }

                return _mGraphics;
            }
        }


        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (transition == Transition.None) return;

            var color = state switch
            {
                SelectionState.Normal => colors.normalColor,
                SelectionState.Highlighted => colors.highlightedColor,
                SelectionState.Pressed => colors.pressedColor,
                SelectionState.Disabled => colors.disabledColor,
                SelectionState.Selected => colors.selectedColor,
                _ => Color.black
            };

            if (!gameObject.activeInHierarchy) return;
            if (transition == Transition.ColorTint) ColorTween(color * colors.colorMultiplier, instant);
        }


        private void ColorTween(Color targetColor, bool instant)
        {
            if (targetGraphic == null) return;
            foreach (var g in Graphics)
            {
                g.CrossFadeColor(targetColor, !instant ? colors.fadeDuration : 0f, true, true);
                if ( g is TextMeshProUGUI text ) {
                    text.ForceMeshUpdate();
                }
            }
        }
    }
}