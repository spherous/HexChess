using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;

public class Multiplayer : MonoBehaviour
{
    [SerializeField] private GameObject whiteKeys;
    [SerializeField] private GameObject blackKeys;
    [SerializeField] private Timers timers;
    [SerializeField] private TurnChangePanel turnChangePanel;
    [SerializeField] private TurnHistoryPanel historyPanel;
    Networker networker;
    Board board;
    LastMoveTracker moveTracker;

    public GameParams gameParams { get; private set; }
    public Team localTeam => gameParams.localTeam;

    private void Awake()
    {
        networker = GameObject.FindObjectOfType<Networker>();
        board = GameObject.FindObjectOfType<Board>();
        moveTracker = GameObject.FindObjectOfType<LastMoveTracker>();
    }

    public void SetupGame(GameParams gameParams)
    {
        Debug.Log($"Setting up game for {gameParams.localTeam} team.");
        this.gameParams = gameParams;

        SmoothHalfOrbitalCamera cam = GameObject.FindObjectOfType<SmoothHalfOrbitalCamera>();
        if(cam)
            cam.Team = gameParams.localTeam;

        whiteKeys.SetActive(gameParams.localTeam == Team.White);
        blackKeys.SetActive(gameParams.localTeam == Team.Black);

        board.currentGame.ChangeTimeParams(gameParams.timerDuration);

        if(gameParams.timerDuration > 0)
        {
            // Game has a timer
            timers.Toggle(true);
            timers.SetTimers(gameParams.timerDuration);
            
        }
        else if(PlayerPrefs.GetInt("ShowClock", true.BoolToInt()).IntToBool())
        {
            // Game has no timer, fall back to user clock preferences
            timers.Toggle(true);
            timers.SetClock();
        }
        else
            timers.Toggle(false);

        if(gameParams.localTeam == Team.White)
            turnChangePanel.Display(gameParams.localTeam);
    }

    public void ReceiveBoard(BoardState state)
    {
        Debug.Log($"Received boardstate: {state.currentMove}'s turn");
        // If a board state is received and the history panel is displaying a previous move, jump to current move then accept new board state
        if(historyPanel.currentTurnPointer != historyPanel.panelPointer)
            historyPanel.JumpToPresent();

        if(board.GetCurrentTurn() == gameParams.localTeam)
            return;

        // Advance Turn is going to flip the current move to the next team
        // So we want to display the turn change panel if it's going to become our turn    
        if(state.currentMove != gameParams.localTeam)
            turnChangePanel.Display(gameParams.localTeam);

        board.SetBoardState(state, board.currentGame.GetTurnCount() + 1);
        board.AdvanceTurn(state, false);

        if(board.currentGame.endType == GameEndType.Pending)
            moveTracker.UpdateText(board.currentGame.GetLastMove());
        else
        {
            List<BoardState> hist = board.currentGame.turnHistory.Skip(board.currentGame.turnHistory.Count - 3).Take(2).ToList();
            moveTracker.UpdateText(HexachessagonEngine.GetLastMove(hist, board.currentGame.promotions));
        }
    }

    public void SendBoard(BoardState state)
    {
        state.executedAtTime = board.currentGame.CurrentTime;
        networker.SendMessage(
            new Message(MessageType.BoardState, state.Serialize())
        );
    }

    public void Surrender(Team surrenderingTeam, float timestamp)
    {
        if(board.currentGame.endType == GameEndType.Pending)
            board.currentGame.Surrender(surrenderingTeam, timestamp);
    }
    public void Surrender(Team surrenderingTeam) =>
        Surrender(surrenderingTeam, board.currentGame.CurrentTime);

    public void Draw(float timestamp) =>
        board.currentGame.EndGame(GameEndType.Draw, Winner.Draw, timestamp);
    public void ClaimDraw() =>
        networker.RespondToDrawOffer(MessageType.AcceptDraw);

    public void SendGameEnd(float timestamp, MessageType endType) =>
        networker.SendMessage(new Message(endType, BitConverter.GetBytes(timestamp)));
    public void ReceiveCheckmate(float timestamp) => board.EndGame(
        timestamp,
        GameEndType.Checkmate,
        gameParams.localTeam == Team.White ? Winner.White : Winner.Black
    );

    public void ReceiveStalemate(float timestamp) =>
        board.EndGame(timestamp, GameEndType.Stalemate, Winner.None);

    public void SendFlagfall(Flagfall flagfall) =>
        networker.SendMessage(new Message(MessageType.FlagFall, flagfall.Serialize()));
    public void ReceiveFlagfall(Flagfall flagfall) => board.EndGame(
        flagfall.timestamp,
        GameEndType.Flagfall,
        flagfall.flaggedTeam == Team.White ? Winner.Black : Winner.White
    );

    public void SendPromote(Promotion promo)
    {
        BoardState state = board.GetCurrentBoardState();
        // state.currentMove = state.currentMove == Team.White ? Team.Black : Team.White;
        networker.SendMessage(new Message(MessageType.Promotion, promo.Serialize()));
        SendBoard(state);
    }
    public void ReceivePromotion(Promotion promo)
    {
        Debug.Log($"promo received for turn {promo.turnNumber}");
        if(board.activePieces.ContainsKey((promo.team, promo.from)))
        {
            IPiece piece = board.activePieces[(promo.team, promo.from)];
            if(piece is Pawn pawn)
            {
                board.PromoteIPiece(pawn, promo.to);
                board.currentGame.AddPromotion(promo);
            }
        }
    }
}