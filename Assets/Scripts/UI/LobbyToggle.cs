using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Toggle toggle;
    public Sprite on;
    public Sprite off;
    public Sprite hovered;

    private void Start() {
        toggle.image.sprite = toggle.isOn ? on : off;
        toggle.onValueChanged.AddListener(isOn => {
            toggle.image.sprite = isOn ? on : off;
        });
    }

    public void OnPointerEnter(PointerEventData eventData) => toggle.image.sprite = hovered;
    public void OnPointerExit(PointerEventData eventData) => toggle.image.sprite = toggle.isOn ? on : off;
}