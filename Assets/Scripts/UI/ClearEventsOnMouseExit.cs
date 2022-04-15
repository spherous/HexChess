using Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClearEventsOnMouseExit : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData){}
    public void OnPointerExit(PointerEventData eventData) => EventSystem.current.Deselect();
}