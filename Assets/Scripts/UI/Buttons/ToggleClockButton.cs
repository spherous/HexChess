using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class ToggleClockButton : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Timers timers;

    private void Awake() {
        bool shouldBeOn = PlayerPrefs.GetInt("ShowClock", true.BoolToInt()).IntToBool();
        if(!shouldBeOn)
        {
            toggle.isOn = false;
            timers.Disable();
        }

        toggle.onValueChanged.AddListener(isOn => {
            PlayerPrefs.SetInt("ShowClock", isOn.BoolToInt());
            
            if(timers.isClock && !isOn)
            {
                timers.isClock = false;
                timers.Toggle(false);
            }
            else if(!timers.isClock && isOn)
            {
                timers.Toggle(true);
                timers.SetClock();
            }
        });

        if(toggle.isOn != shouldBeOn)
            toggle.isOn = shouldBeOn;
    }
}