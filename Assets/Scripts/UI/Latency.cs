using UnityEngine;
using TMPro;
using Extensions;

public class Latency : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI latencyText;
    [SerializeField] private GroupFader fader;

    private void Awake() {
        if(!PlayerPrefs.GetInt("Latency", true.BoolToInt()).IntToBool())
            fader.Disable();
    }

    public void UpdateLatency(int latencyMs) => latencyText.text = $"{latencyMs} ms";

    public void Toggle(bool isOn)
    {
        if(isOn && !fader.visible)
            fader.FadeIn();
        else if(!isOn && fader.visible)
            fader.FadeOut();
    }
}