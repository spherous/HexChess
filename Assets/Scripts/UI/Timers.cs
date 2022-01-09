using UnityEngine;
using TMPro;
using System;

public class Timers : MonoBehaviour
{
    Board board;

    [SerializeField] private TextMeshProUGUI whiteTimerText;
    [SerializeField] private TextMeshProUGUI blackTimerText;

    public bool isClock = false;
    public float timerDruation {get; private set;}    

    Team currentTurn;

    private void Awake() {
        board = GameObject.FindObjectOfType<Board>();
        board.newTurn += NewTurn;
        board.gameOver += GameOver;
        
        if(isClock)
        {
            UpdateClockUI(0, Team.White);
            UpdateClockUI(0, Team.Black);
        }
        else if(timerDruation > 0)
            UpdateBothTimers();
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
        if(currentTurn == Team.None)
        {
            if(board == null)
                board = GameObject.FindObjectOfType<Board>();
            currentTurn = board.GetCurrentTurn();
        }

        board.currentGame.ChangeTimeParams(true, 0);

        isClock = true;
        timerDruation = 0;
        UpdateBothUI();
    }

    public void SetTimers(float duration)
    {
        if(currentTurn == Team.None)
        {
            if(board == null)
                board = GameObject.FindObjectOfType<Board>();
            currentTurn = board.GetCurrentTurn();
        }

        board.currentGame.ChangeTimeParams(false, duration);

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

    string GetFormat(float seconds) => seconds < 60 
        ? @"%s\.f" 
        : seconds < 3600
            ? @"%m\:ss\.f"
            : @"%h\:mm\:ss\.f";
        
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

        GetTeamText(team).text = TimeSpan.FromSeconds(seconds).ToString(GetFormat(seconds));
    }

    private void UpdateTimerUI(float seconds, Team team)
    {
        if(team == Team.None)
            return;
        float remaining = timerDruation - seconds;
        string teamTime = TimeSpan.FromSeconds(remaining).ToString(GetFormat(remaining));
        string td = TimeSpan.FromSeconds(timerDruation).ToString(GetFormat(timerDruation));
        
        GetTeamText(team).text = $"{teamTime} / {td}";
    }
}