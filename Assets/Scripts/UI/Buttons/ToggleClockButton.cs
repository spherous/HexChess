using UnityEngine;
using UnityEngine.UI;

public class ToggleClockButton : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Timers timers;

    private void Awake() {
        toggle.onValueChanged.AddListener(isOn => {
            if(timers.isClock)
            {
                timers.isClock = false;
                timers.gameObject.SetActive(false);
            }
            else
            {
                timers.gameObject.SetActive(true);
                timers.SetClock();
            }
        });
    }
}