using System;
using Extensions;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovePanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnNumberText;
    [SerializeField] private TextMeshProUGUI whiteText;
    [SerializeField] private TextMeshProUGUI blackText;
    [SerializeField] private TextMeshProUGUI whiteTimestamp;
    [SerializeField] private TextMeshProUGUI blackTimestamp;
    [SerializeField] private TextMeshProUGUI whiteDeltaTime;
    [SerializeField] private TextMeshProUGUI blackDeltaTime;
    [SerializeField] private Image whiteBG;
    [SerializeField] private Image blackBG;
    [SerializeField] private Image whiteHighlighter;
    [SerializeField] private Image blackHighlighter;
    public Color lightOrange;
    public Color darkOrange;
    public Color darkA;
    public Color darkB;
    public Color lightA;
    public Color lightB;
    Board board;
    TurnHistoryPanel historyPanel;
    public BoardState whiteState {get; private set;}
    public BoardState blackState {get; private set;}
    [ShowInInspector] public Move whiteMove {get; private set;}
    [ShowInInspector] public Move blackMove {get; private set;}
    public int index {get; private set;}

    bool whiteSet = false;
    bool blackSet = false;

    private void Awake()
    {
        board = GameObject.FindObjectOfType<Board>();
        historyPanel = GameObject.FindObjectOfType<TurnHistoryPanel>();
    }
    public void SetIndex(int index) => this.index = index;
    public void SetMove(BoardState state, Move move, NotationType notationType)
    {
        if(move.lastTeam == Team.White && !whiteSet)
        {
            whiteSet = true;
            whiteState = state;
            whiteMove = move;
        }
        else if(move.lastTeam == Team.Black && !blackSet)
        {
            blackSet = true;
            blackState = state;
            blackMove = move;
        }

        TextMeshProUGUI toChange = move.lastTeam == Team.White ? whiteText : blackText;
        string notation = Notation.Get(state, move, board.currentGame.promotions, notationType);
        
        toChange.text = notation;

        if(move.lastTeam == Team.White)
            blackText.text = "";
        
        TextMeshProUGUI deltaTimeToChange = move.lastTeam == Team.White ? whiteDeltaTime : blackDeltaTime;
        if(deltaTimeToChange != null)
            deltaTimeToChange.text = $"+{TimeSpan.FromSeconds(move.duration).ToString(move.duration.GetStringFromSeconds())}";
    }

    public void SetNotation(NotationType type)
    {
        if(whiteSet)
            whiteText.text = Notation.Get(whiteState, whiteMove, board.currentGame.promotions, type);
        
        if(blackSet)
            blackText.text = Notation.Get(blackState, blackMove, board.currentGame.promotions, type);
    }

    public void SetTimestamp(float seconds, Team team)
    {
        TextMeshProUGUI toChange = team == Team.White ? whiteTimestamp : blackTimestamp;
        toChange.text = TimeSpan.FromSeconds(seconds).ToString(seconds.GetStringFromSeconds());
    }
    public void ClearTimestamp(Team team)
    {
        TextMeshProUGUI toChange = team == Team.White ? whiteTimestamp : blackTimestamp;
        toChange.text = "";
    }
    public void ClearDeltaTime(Team team)
    {
        TextMeshProUGUI deltaTimeToChange = team == Team.White ? whiteDeltaTime : blackDeltaTime;
        if(deltaTimeToChange == null)
            return;
        deltaTimeToChange.text = "";
    }

    public void SetTurnNumber(int val) => turnNumberText.text = $"{val}";
    public void SetDark()
    {
        whiteBG.color = lightB;
        blackBG.color = darkB;
    }
    public void SetLight()
    {
        whiteBG.color = lightA;
        blackBG.color = darkA;
    }
    public void FlipColor()
    {
        whiteBG.color = whiteBG.color == lightA ? lightB : lightA;
        blackBG.color = blackBG.color == darkA ? darkB : darkA;
    }
    public void HighlightTeam(Team team)
    {
        Image toChange = team == Team.White ? whiteHighlighter : blackHighlighter;
        Image other = team == Team.White ? blackHighlighter : whiteHighlighter;
        toChange.enabled = true;
        other.enabled = false;

        if(team == Team.White)
            whiteDeltaTime.color = lightOrange;
        else if(team == Team.Black && whiteDeltaTime.color == lightOrange)
            whiteDeltaTime.color = darkOrange;
    }

    public void ClearHighlight()
    {
        if(whiteHighlighter == null || blackHighlighter == null)
            return;
        
        if(whiteDeltaTime.color != darkOrange)
            whiteDeltaTime.color = darkOrange;
            
        whiteHighlighter.enabled = false;
        blackHighlighter.enabled = false;
    }
    public BoardState GetState(Team team) => team == Team.White ? whiteState : blackState;
    public bool TryGetState(Team team, out BoardState state)
    {
        state = GetState(team);
        return state.allPiecePositions != null;
    }
    public Move GetMove(Team team) => team == Team.White ? whiteMove : blackMove;

    public void SetHistory(Team team) 
    {
        if(GetMove(team).lastTeam == Team.None)
            return;

        historyPanel.HistoryJump((index, team));
    } 
}