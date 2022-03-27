using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Toggle toggle;
    public Sprite on;
    public Sprite off;
    public Sprite hovered;

    [SerializeField] private Image icon;
    [SerializeField] private Image iconBG;

    [SerializeField] private TextMeshProUGUI text;

    public Color activeIconColor;
    public Color inactiveIconColor;
    public Color activeBGColor;
    public Color inactiveBGColor;

    private void Start() {
        toggle.image.sprite = toggle.isOn ? on : off;
        
        if(icon != null)
            icon.color = toggle.isOn ? activeIconColor : inactiveIconColor;
        if(iconBG != null)
            iconBG.color = toggle.isOn ? activeBGColor : inactiveBGColor;

        toggle.onValueChanged.AddListener(isOn => {
            toggle.image.sprite = isOn ? on : off;
            if(icon != null)
                icon.color = isOn ? activeIconColor : inactiveIconColor;
            if(iconBG != null)
                iconBG.color = isOn ? activeBGColor : inactiveBGColor;
        });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        toggle.image.sprite = hovered;

        if(text != null)
            text.color = activeBGColor;
    } 
    public void OnPointerExit(PointerEventData eventData)
    {
        toggle.image.sprite = toggle.isOn ? on : off;

        if(text != null)
            text.color = activeIconColor;
    } 
}