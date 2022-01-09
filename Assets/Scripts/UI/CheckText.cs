using UnityEngine;
using TMPro;
using System;

public class CheckText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    Board board;

    private void Awake() {
        board = GameObject.FindObjectOfType<Board>();
        board.newTurn += NewTurn;
        board.gameOver += GameOver;
        gameObject.SetActive(false);
    }

    private void GameOver(Game game)
    {
        if(game.winner == Winner.Pending)
            return;
            
        gameObject.SetActive(true);
        text.color = Color.red;

        BoardState finalState = game.turnHistory[game.turnHistory.Count - 1];
        Team loser = game.winner == Winner.White ? Team.Black : Team.White;

        text.text = game.endType switch {
            GameEndType.Pending => SupportOldSaves(game),
            GameEndType.Draw => "",
            GameEndType.Checkmate => "Checkmate.",
            GameEndType.Surrender => $"{loser} surrendered.",
            GameEndType.Flagfall => $"{loser} flagfell.",
            GameEndType.Stalemate => "Stalemate.",
            _ => $"{loser} has lost."
        };
    }

    private string SupportOldSaves(Game game)
    {
        Team loser = game.winner == Winner.White ? Team.Black : Team.White;

        if(game.winner == Winner.Pending)
            return "";
        else if(game.winner == Winner.Draw)
            return "Draw.";
        else if(game.turnHistory[game.turnHistory.Count - 1].checkmate > Team.None)
            return "Checkmate.";
        else
            return $"{loser} surrendered.";
    }

    private void NewTurn(BoardState newState)

    {
        if(newState.check == Team.None && newState.checkmate == Team.None)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        if(newState.check > 0)
        {
            text.color = Color.yellow;
            text.text = "Check";
        }
        else if(newState.checkmate > 0)
        {
            text.color = Color.red;
            text.text = "Checkmate";
        }
        else
            text.text = "Something went wrong";
    }


}
