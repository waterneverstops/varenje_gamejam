using UnityEngine;
using UnityEngine.EventSystems;

public class HoverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject buttonLayer;
    [SerializeField] private GameObject hoverLayer;

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonLayer.SetActive(false);
        hoverLayer.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonLayer.SetActive(true);
        hoverLayer.SetActive(false);
    }
}
