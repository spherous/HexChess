using UnityEngine;
using TMPro;
using System;
using Extensions;

public class Timers : MonoBehaviour
{
    Board board;

    [SerializeField] private TextMeshProUGUI whiteTimerText;
    [SerializeField] private TextMeshProUGUI blackTimerText;
    [SerializeField] private TextMeshProUGUI timeControlText;
    [SerializeField] private GroupFader fader;

    [SerializeField] private TMP_FontAsset regFont;
    [SerializeField] private TMP_FontAsset boldFont;

    public bool isClock = false;
    public float timerDruation {get; private set;}    

    Team currentTurn;

    private void Awake() {
        board = GameObject.FindObjectOfType<Board>();
        board.newTurn += NewTurn;
        board.gameOver += GameOver;        
        
        if(isClock)
        {
            timeControlText.font = regFont;
            timeControlText.text = "No Time Control";
            UpdateClockUI(0, Team.White);
            UpdateClockUI(0, Team.Black);
        }
        else if(timerDruation > 0)
        {
            timeControlText.font = boldFont;
            timeControlText.text = TimeSpan.FromSeconds(timerDruation).ToString(timerDruation.GetStringFromSeconds());
            UpdateBothTimers();
        }
    }

    private void GameOver(Game game)
    {
        if(isClock)
            UpdateBothClocks();
        else if(timerDruation > 0)
            UpdateBothTimers();
    } 

    public void SetClock()
    {
        // clocks are only displayed when the game does not have time controls
        if(timerDruation > 0)
            return;
        
        if(!PlayerPrefs.GetInt("ShowClock", 1).IntToBool())
        {
            isClock = false;
            fader.FadeOut();
            return;
        }

        isClock = true;
        timeControlText.font = regFont;
        timeControlText.text = "No Time Controls";
        
        if(currentTurn == Team.None)
        {
            if(board == null)
                board = GameObject.FindObjectOfType<Board>();
            currentTurn = board.GetCurrentTurn();
        }

        UpdateBothUI();
    }

    public void ClearTimer()
    {
        timerDruation = 0;
        SetClock();
    }

    public void SetTimers(float duration)
    {
        timeControlText.font = boldFont;
        timeControlText.text = $"{TimeSpan.FromSeconds(duration).ToString(duration.GetStringFromSeconds())}";
        if(currentTurn == Team.None)
        {
            if(board == null)
                board = GameObject.FindObjectOfType<Board>();
            currentTurn = board.GetCurrentTurn();
        }

        board.currentGame.ChangeTimeParams(duration);

        isClock = false;
        timerDruation = duration;
        UpdateBothTimers();
    }

    private void NewTurn(BoardState newState)
    {
        currentTurn = newState.currentMove;
        UpdateBothUI();
    }

    public void UpdateBothUI()
    {
        if(isClock)
            UpdateBothClocks();
        else if(timerDruation > 0)
            UpdateBothTimers();
    }

    private void UpdateBothClocks()
    {
        UpdateClockUI(GetTeamTime(Team.White), Team.White);
        UpdateClockUI(GetTeamTime(Team.Black), Team.Black);
    }
    private void UpdateBothTimers()
    {
        UpdateTimerUI(GetTeamTime(Team.White), Team.White);
        UpdateTimerUI(GetTeamTime(Team.Black), Team.Black);
    }

    private void Update()
    {
        if(currentTurn == Team.None)
            return;

        if(isClock)
            UpdateClockUI(GetTeamTime(currentTurn), currentTurn);
        else if(timerDruation > 0)
            UpdateTimerUI(GetTeamTime(currentTurn), currentTurn);
    }
        
    TextMeshProUGUI GetTeamText(Team team) => team == Team.White ? whiteTimerText : blackTimerText;
    float GetTeamTime(Team team)
    {
        Timekeeper whiteKeeper = board.currentGame.whiteTimekeeper;
        Timekeeper blackKeeper = board.currentGame.blackTimekeeper;
        
        whiteKeeper.rwl.AcquireReaderLock(1);
        blackKeeper.rwl.AcquireReaderLock(1);
        float time = team == Team.White ? whiteKeeper.elapsed : blackKeeper.elapsed;
        whiteKeeper.rwl.ReleaseReaderLock();
        blackKeeper.rwl.ReleaseReaderLock();
    
        return time;
    } 

    public void UpdateClockUI(float seconds, Team team)
    {
        if(team == Team.None)
            return;

        GetTeamText(team).text = TimeSpan.FromSeconds(seconds).ToString(seconds.GetStringFromSeconds());
    }

    private void UpdateTimerUI(float seconds, Team team)
    {
        if(team == Team.None)
            return;
        float remaining = timerDruation - seconds;
        string teamTime = TimeSpan.FromSeconds(remaining).ToString(remaining.GetStringFromSeconds());
        
        GetTeamText(team).text = $"{teamTime}";
    }

    public void Toggle(bool isOn)
    {
        if(isOn && !fader.visible)
            fader.FadeIn();
        else if(!isOn && fader.visible)
            fader.FadeOut();
    }

    public void Disable() => fader.Disable();
}