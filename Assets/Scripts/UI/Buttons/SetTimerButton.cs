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
    [SerializeField] private Board board;
    public int defaultTimerLength;

    private void Awake() {
        board.newTurn += OnNewTurn;
        board.gameOver += OnGameOver;

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
                
                timers.Toggle(true);

                UpdateTimers(int.Parse(minuteInput.text));
            }
            else
            {
                timerText.rectTransform.sizeDelta = new Vector2(131, timerText.rectTransform.sizeDelta.y);
                timerText.text = "Timer (off)";
                minuteInput.gameObject.SetActive(false);

                timers.ClearTimer();
            }

            EventSystem.current.Deselect();
        });
    }

    private void OnGameOver(Game game)
    {
        if(gameObject.activeSelf)
        {
            timerToggle.isOn = false;
            gameObject.SetActive(false);
        }
    }

    private void OnNewTurn(BoardState newState)
    {
        // Note we use currentGame.GetTurnCount instead of board.GetTurnCount.
        // Because board.GetTurnCount accounts for what turn the player may be looking at in history
        // But we don't care about the history, only the most current turn count.
        int? turnCount = board.currentGame?.GetTurnCount(); 
        if(turnCount.HasValue && turnCount >= 1)
            gameObject.SetActive(false);
        else if(turnCount.HasValue && turnCount == 0)
        {
            // reset because default board is loaded
            gameObject.SetActive(true);
            timerToggle.isOn = false;
        }
    }

    public void UpdateTimers(int minutes)
    {
        timers.SetTimers(minutes * 60);
        timers.UpdateBothUI();
    }
}