using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TwigglyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [SerializeField] private ButtonArrowAnim arrowAnim;
    [SerializeField] private Image image;
    [SerializeField] private AudioSource audioSource;
    public List<AudioClip> clips = new List<AudioClip>();
    public Sprite normalState;
    public Sprite hoveredState;
    public Sprite selectedState;

    public delegate void OnClick();
    public OnClick onClick;

    List<TwigglyButton> otherTwigglyButtons = new List<TwigglyButton>();

    protected void Awake() {
        image.sprite = normalState;
        otherTwigglyButtons = GameObject.FindObjectsOfType<TwigglyButton>().Where(b => b != this).ToList();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // set selected
        arrowAnim.Hide();
        image.sprite = selectedState;
        foreach(var tb in otherTwigglyButtons)
            tb.SetNorm();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // set 
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // start hover animation
        image.sprite = hoveredState;
        audioSource.PlayOneShot(clips.ChooseRandom());
        arrowAnim.Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // stop animation, to go normal or selected depending if clicked
        if(image.sprite != selectedState)
            image.sprite = normalState;
        arrowAnim.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }

    public void SetNorm()
    {
        image.sprite = normalState;
    }
}