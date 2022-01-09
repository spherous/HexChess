using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerNudgeButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_InputField timerInput;
    public bool isIncrement = true;

    private void Awake() {
        button.onClick.AddListener(() => {
            if(int.TryParse(timerInput.text, out int val))
            {
                val = isIncrement ? Mathf.Min(val + 1, int.MaxValue) : Mathf.Max(val - 1, 1);
                timerInput.text = $"{val}";
            }
        });
    }
}