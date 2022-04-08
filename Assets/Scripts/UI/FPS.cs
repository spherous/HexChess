using Extensions;
using TMPro;
using UnityEngine;

public class FPS : MonoBehaviour
{
    public bool displayFPS {get; private set;}
    [SerializeField] private TextMeshProUGUI fpsCounter;
    [SerializeField] private GroupFader fader;

    public float fpsCounterUpdateDelay;
    private float updateCounterAtTime;

    private float dt;

    private void Update()
    {
        // If dt isn't calculated every frame, the fps counter will be inaccurate when being re-enabled, because dt will have stale information.
        dt += (Time.unscaledDeltaTime - dt) * .1f;

        if(!displayFPS)
            return;
        
        if(Time.unscaledTime >= updateCounterAtTime)
        {
            fpsCounter.text = $"{Mathf.Clamp((1f/dt).Floor(), 0, 9999)}";
            updateCounterAtTime = Time.unscaledTime + fpsCounterUpdateDelay;
        }
    }

    public void Toggle(bool isOn)
    {
        displayFPS = isOn;
        PlayerPrefs.SetInt("ShowFPS", isOn.BoolToInt());

        if(isOn)
            fader?.FadeIn();
        else
            fader?.FadeOut();
    }
}