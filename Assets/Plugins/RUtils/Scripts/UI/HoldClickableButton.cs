using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Plugins.RProjects.RUtils.Scripts.UI
{
    public class HoldClickableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _holdDuration;

        public event Action OnPointerReleased;
        public event Action OnClick;
        public event Action OnHoldClicked;

        private float _elapsedTime;

        public float HoldProgress => _elapsedTime / _holdDuration;

        public bool IsHoldingButton { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => ToggleHoldingButton(true);

        private void ToggleHoldingButton(bool isPointerDown)
        {
            IsHoldingButton = isPointerDown;

            if (!isPointerDown) return;
            _elapsedTime = 0;
            OnClick?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ManageButtonInteraction(true);
            ToggleHoldingButton(false);
        }

        private void ManageButtonInteraction(bool isPointerUp = false)
        {
            if (!IsHoldingButton)
                return;

            if (isPointerUp)
            {
                PointerUp();
                return;
            }

            _elapsedTime += Time.deltaTime;
            var isHoldClickDurationReached = _elapsedTime > _holdDuration;

            if (isHoldClickDurationReached)
                HoldClick();
        }

        private void PointerUp()
        {
            OnPointerReleased?.Invoke();
        }

        private void HoldClick()
        {
            ToggleHoldingButton(false);
            OnHoldClicked?.Invoke();
        }

        private void Update() => ManageButtonInteraction();
    }
}