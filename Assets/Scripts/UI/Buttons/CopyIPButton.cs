using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CopyIPButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image copyImage;
    [SerializeField] private TextMeshProUGUI copyText;

    public Color hoverColor;
    public Color pressedColor;

    public void SetColor(Color toSet)
    {
        copyImage.color = toSet;
        copyText.color = toSet;
    }
    
    public void OnPointerEnter(PointerEventData eventData) => SetColor(hoverColor);
    public void OnPointerExit(PointerEventData eventData) => SetColor(Color.white);
    public void OnPointerDown(PointerEventData eventData) => SetColor(pressedColor);
    public void OnPointerUp(PointerEventData eventData) => SetColor(copyImage.color == pressedColor ? hoverColor : Color.white);
}