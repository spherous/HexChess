using System.Collections.Generic;
using Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class AudioUI : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private AudioSource source;
    public bool canPlay = true;
    public List<AudioClip> clips = new List<AudioClip>();
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(canPlay)
            source.PlayOneShot(clips.ChooseRandom());
    } 
}