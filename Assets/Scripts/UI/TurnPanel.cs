using UnityEngine;
using TMPro;
using System;
using Extensions;

public class TurnPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private SurrenderButton surrenderButton;
    GameObject mainMenuButton;
    [SerializeField] private GameObject mainMenuButtonPrefab;

    Board board;
    Multiplayer multiplayer;
    private void Awake() {
        board = GameObject.FindObjectOfType<Board>();
        board.newTurn += NewTurn;
        board.gameOver += GameOver;
        multiplayer = GameObject.FindObjectOfType<Multiplayer>();
    }

    public void GameOver(Game game)
    {
        SetGameEndText(game);
        
        if(mainMenuButton == null)
            mainMenuButton = Instantiate(mainMenuButtonPrefab, buttonContainer);
    }

    public void SetGameEndText(Game game)
    {
        turnText.color = game.winner switch {
            Winner.White => Color.white,
            Winner.Black => Color.black,
            _ => Color.red
        };

        Team loser = game.winner == Winner.White ? Team.Black : Team.White;
        float gameLength = game.GetGameLength();
        string formattedGameLength = TimeSpan.FromSeconds(gameLength).ToString(gameLength.GetStringFromSeconds());
        int turnCount = game.GetTurnCount();
        string durationString = game.timerDuration == 0 && !game.hasClock 
            ? $"Game over! On turn {turnCount}" 
            : $"Game over! On turn {turnCount} in {formattedGameLength}";

        turnText.text = game.endType switch {
            // game.endType was added in v1.0.8 to support flagfalls and stalemates, any game saves from before then will default to Pending
            GameEndType.Pending => SupportOldSaves(game),
            GameEndType.Draw => $"{durationString}, a draw has occured.",
            GameEndType.Checkmate => $"{durationString} {game.winner} has won by checkmate!",
            GameEndType.Surrender => $"{durationString} {game.winner} has won by surrender.",
            GameEndType.Flagfall => $"{durationString} {game.winner} has flagged {loser}.",
            GameEndType.Stalemate => $"{durationString} a stalemate has occured.",
            _ => $"{durationString} {game.winner} is victorius!"
        };
    }

    private string SupportOldSaves(Game game)
    {
        int turnCount = game.GetTurnCount();
        // We can figure out most of what we need here, including if it actually is a pending game
        string turnPlurality = turnCount > 1 ? "turns" : "turn";
        if(game.winner == Winner.Pending)
            return "";
        else if(game.winner == Winner.Draw)
            return $"After {turnCount} {turnPlurality}, both teams have agreed to a draw.";
        else if(game.turnHistory[game.turnHistory.Count - 1].checkmate > Team.None)
            return $"After {turnCount} {turnPlurality}, {game.winner} has won by checkmate!";
        else
            return $"After {turnCount} {turnPlurality}, {game.winner} has won by surrender.";
    }

    public void NewTurn(BoardState newState)
    {
        int turnCount = board.currentGame.GetTurnCount() + board.currentGame.turnHistory.Count % 2;
        NewTurn(newState, turnCount);
    }

    public void NewTurn(BoardState newState, int turnCount)
    {
        string text = multiplayer == null 
            ? newState.currentMove == Team.White ? "White's Turn" : "Black's Turn"
            : newState.currentMove == multiplayer.localTeam ? "Your Turn" : "Opponent's Turn";

        turnText.text = $"{turnCount}:{text}";
        turnText.color = newState.currentMove == Team.White ? Color.white : Color.black;
    }

    public void Reset()
    {
        if(mainMenuButton != null)
            Destroy(mainMenuButton);
    }
}