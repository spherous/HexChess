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
    [SerializeField] private AudioSource audioSource;
    [SerializeField] protected TextMeshProUGUI text;
    public bool changeTextColor = true;
    public Color hoverColor;
    public Color normalTextColor = Color.white;
    public List<AudioClip> clips = new List<AudioClip>();
    public Sprite normalState;
    public Sprite hoveredState;
    public Sprite selectedState;

    public delegate void OnClick();
    public OnClick onClick;

    List<TwigglyButton> otherTwigglyButtons = new List<TwigglyButton>();

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
            image.sprite = selectedState;
        foreach(var tb in otherTwigglyButtons)
            tb.SetNorm();
    }

    public void OnPointerUp(PointerEventData eventData){}

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(changeTextColor && text != null)
            text.color = hoverColor;
        else if(!changeTextColor && image != null)
            image.color = hoverColor;

        // start hover animation
        if(image != null)
            image.sprite = hoveredState;
        audioSource?.PlayOneShot(clips.ChooseRandom());
        arrowAnim?.Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(changeTextColor && text != null)
            text.color = normalTextColor;
        else if(!changeTextColor && image != null)
            image.color = normalTextColor;

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