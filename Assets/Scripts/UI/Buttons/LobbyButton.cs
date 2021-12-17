using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private Button button;
    public Sprite on;
    public Sprite off;
    public Sprite hovered;

    public void OnPointerEnter(PointerEventData eventData) => button.image.sprite = hovered;
    public void OnPointerExit(PointerEventData eventData) => button.image.sprite = off;
    public void OnPointerDown(PointerEventData eventData) => button.image.sprite = on;
}