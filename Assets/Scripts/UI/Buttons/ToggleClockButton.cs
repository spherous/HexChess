using UnityEngine;
using UnityEngine.UI;

public class ToggleClockButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Timers timers;

    private void Awake() {
        button.onClick.AddListener(() => {
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