using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Extensions;

public class SetTimerButton : MonoBehaviour
{
    [SerializeField] private Toggle timerToggle;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TMP_InputField minuteInput;
    [SerializeField] private Timers timers;
    public int defaultTimerLength;

    private void Awake() {
        minuteInput.gameObject.SetActive(false);
        timerText.rectTransform.sizeDelta = new Vector2(131, timerText.rectTransform.sizeDelta.y);
        
        minuteInput.onValueChanged.AddListener(value => {
            if(value.Length == 0)
                UpdateTimers(defaultTimerLength);
            else
                UpdateTimers(int.Parse(minuteInput.text));
        });

        timerToggle.onValueChanged.AddListener(isOn => {
            if(isOn)
            {
                timerText.text = "Timer (mins)";
                timerText.rectTransform.sizeDelta = new Vector2(150, timerText.rectTransform.sizeDelta.y);
                minuteInput.gameObject.SetActive(true);

                if(string.IsNullOrEmpty(minuteInput.text))
                    minuteInput.text = $"{defaultTimerLength}";
                
                if(!timers.gameObject.activeSelf)
                    timers.gameObject.SetActive(true);

                UpdateTimers(int.Parse(minuteInput.text));
            }
            else
            {
                timerText.rectTransform.sizeDelta = new Vector2(131, timerText.rectTransform.sizeDelta.y);
                timerText.text = "Timer (off)";
                minuteInput.gameObject.SetActive(false);

                timers.SetClock();
            }

            EventSystem.current.Deselect();
        });
    }

    public void UpdateTimers(int minutes)
    {
        timers.SetTimers(minutes * 60);
        timers.UpdateBothUI();
    }
}