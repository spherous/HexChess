using System.Collections.Generic;
using Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MuteButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Slider slider;
    [SerializeField] private AdjustMixerGroupVolume mixerGroupVolume;
    [SerializeField] private AudioSource source;
    public List<AudioClip> clips = new List<AudioClip>();
    public Color orangeColor;
    public Sprite speaker;
    public Sprite mutedSpeaker;

    float volume = 10;
    private void Start()
    {
        bool isMuted = PlayerPrefs.GetInt("Mute", 1) > 0;
        toggle.isOn = !isMuted;
        toggle.image.sprite = isMuted ? mutedSpeaker : speaker;
        toggle.image.color = isMuted ? Color.gray : Color.white;

        float pref = PlayerPrefs.GetFloat("Volume", volume);
        slider.value = pref * slider.maxValue;

        slider.onValueChanged.AddListener(val => NewVolume(val / slider.maxValue));
        toggle.onValueChanged.AddListener(val =>
        {
            int vol = val.BoolToInt();
            slider.value = vol * slider.maxValue;
            volume = (float)vol;
            SetMute(!val);
        });
    }

    private void SetMute(bool isMuted)
    {
        PlayerPrefs.SetInt("Mute", isMuted.BoolToInt());
        toggle.image.sprite = isMuted ? mutedSpeaker : speaker;
        toggle.image.color = isMuted ? Color.gray : Color.white;
    }

    private void NewVolume(float fillAmount)
    {
        bool isMuted = fillAmount <= 0.0001f;
        fillAmount = fillAmount < 0.0001f ? 0.0001f : Mathf.Clamp01(fillAmount);
        SetMute(isMuted);
        volume = fillAmount;
        mixerGroupVolume.SetVolume(volume);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        toggle.image.color = orangeColor;
        source?.PlayOneShot(clips.ChooseRandom());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        bool isMuted = PlayerPrefs.GetInt("Mute", 1) > 0;
        toggle.image.color = isMuted ? Color.gray : Color.white;
    }
}