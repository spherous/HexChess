using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TwigglyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [SerializeField] private ButtonArrowAnim arrowAnim;
    [SerializeField] private Image image;
    [SerializeField] private Image icon;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] protected TextMeshProUGUI text;
    public bool changeTextColor = true;
    public bool changeColorOnPress = false;
    public Color hoverColor;
    public Color normalTextColor = Color.white;
    public Color pressedColor;
    public List<AudioClip> clips = new List<AudioClip>();
    public Sprite normalState;
    public Sprite hoveredState;
    public Sprite selectedState;

    public delegate void OnClick();
    public OnClick onClick;

    List<TwigglyButton> otherTwigglyButtons = new List<TwigglyButton>();

    bool hovered = false;

    protected void Awake() {
        if(image != null)
            image.sprite = normalState;
        otherTwigglyButtons = GameObject.FindObjectsOfType<TwigglyButton>().Where(b => b != this).ToList();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // set selected
        arrowAnim?.Hide();
        if(image != null)
        {
            image.sprite = selectedState;

            if(changeColorOnPress)
                image.color = pressedColor;
        }
        foreach(var tb in otherTwigglyButtons)
            tb.SetNorm();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(image != null)
        {
            image.sprite = hovered ? hoveredState : normalState;

            if(changeColorOnPress)
                image.color = hovered ? hoverColor : normalTextColor;
        }  
        
        if(hovered)
            arrowAnim?.Show();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;

        if(changeTextColor && text != null)
            text.color = hoverColor;
        else if(!changeTextColor && image != null)
        {
            image.color = hoverColor;
            if(icon != null)
                icon.color = normalTextColor;
        }

        // start hover animation
        if(image != null)
            image.sprite = hoveredState;
        audioSource?.PlayOneShot(clips.ChooseRandom());
        arrowAnim?.Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;

        if(changeTextColor && text != null)
            text.color = normalTextColor;
        else if(!changeTextColor && image != null)
        {
            image.color = normalTextColor;
            if(icon != null)
                icon.color = hoverColor;
        }

        // stop animation, to go normal or selected depending if clicked
        if(image != null && image.sprite != selectedState)
            image.sprite = normalState;
        arrowAnim?.Hide();
    }

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke();

    public void SetNorm()
    {
        if(image == null)
            return;

        if(image != null)
            image.sprite = normalState;
    }
}