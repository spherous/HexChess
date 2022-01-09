using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetTimerButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_InputField minuteInput;
    [SerializeField] private Timers timers;
    public int defaultTimerLength;

    private void Awake() {
        button.onClick.AddListener(() => {
            if(string.IsNullOrEmpty(minuteInput.text))
                minuteInput.text = $"{defaultTimerLength}";
            
            if(!timers.gameObject.activeSelf)
                timers.gameObject.SetActive(true);

            timers.SetTimers(int.Parse(minuteInput.text) * 60);
            timers.UpdateBothUI();
        });
    }
}