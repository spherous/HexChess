using System.Collections.Generic;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AIDifficultySlider : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Mouse mouse => Mouse.current;
    [SerializeField] private Slider slider;
    [SerializeField] private AudioSource source;
    [SerializeField] private TextMeshProUGUI difficultyText;
    public Slider difficultySlider => slider;
    public int AILevel => (int)slider.value + 1;

    public List<AudioClip> clips = new List<AudioClip>();

    bool hovered = false;

    private void Awake() {
        difficultyText.text = $"DIFFICULTY ({slider.value + 1})";
        slider.onValueChanged.AddListener(newVal => {
            difficultyText.text = $"DIFFICULTY ({newVal + 1})";
            source.PlayOneShot(clips.ChooseRandom());
        });
    }

    private void Update() {
        if(hovered)
        {
            Vector2 scrollVal = mouse.scroll.ReadValue();
            if(scrollVal.y < 0 || scrollVal.y > 0)
                Scroll(Mathf.Clamp(scrollVal.y, -1, 1).FloorToInt());
        }
    }

    public void Scroll(int dir)
    {
        if(slider.value + dir >= 0 && slider.value + dir <= slider.maxValue)
            slider.value = slider.value + dir;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }
}