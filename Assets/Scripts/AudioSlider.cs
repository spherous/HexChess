using System.Collections.Generic;
using Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour, IScrollHandler
{
    [SerializeField] private Slider slider;
    [SerializeField] private SliderTicks sliderTicks;
    [SerializeField] private AudioSource source;
    [SerializeField] private List<AudioClip> clips = new List<AudioClip>();

    private void Awake() => sliderTicks.AddTicks();

    public void OnScroll(PointerEventData eventData)
    {
        int initialVal = slider.value.Ceil();
        slider.value = Mathf.Clamp(eventData.scrollDelta.y > 0 ? slider.value + 1 : slider.value - 1, slider.minValue, slider.maxValue);
        if(initialVal != slider.value.Ceil())
            source.PlayOneShot(clips.ChooseRandom());
    }
}