using UnityEngine;
using UnityEngine.EventSystems;

public class HoverSlide : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform hoverImage;

    [SerializeField] private Vector2 hiddenPosition = new(-800f, 0f);
    [SerializeField] private Vector2 visiblePosition = Vector2.zero;

    [SerializeField] private float animationSpeed = 10f;

    private Vector2 targetPosition;

    private void Start()
    {
        hoverImage.anchoredPosition = hiddenPosition;
        targetPosition = hiddenPosition;
    }

    private void Update()
    {
        hoverImage.anchoredPosition = Vector2.Lerp(
            hoverImage.anchoredPosition,
            targetPosition,
            animationSpeed * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPosition = visiblePosition;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPosition = hiddenPosition;
    }
}
