using UnityEngine;
using UnityEngine.Audio;

public class AdjustMixerGroupVolume : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string mixerGroupName;

    private void Start() => SetVolume(PlayerPrefs.GetFloat("Volume", 1));

    public void SetVolume(float silderVal) 
    {
        PlayerPrefs.SetFloat("Volume", silderVal);
        mixer.SetFloat(mixerGroupName, Mathf.Log10(silderVal) * 20);
    }
}