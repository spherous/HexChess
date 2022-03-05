using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class CheckText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Image boarderBar;
    [SerializeField] private Image hexOutline;
    Board board;
    private Color boarderDefaultColor;

    private void Awake() {
        board = GameObject.FindObjectOfType<Board>();
        board.newTurn += NewTurn;
        board.gameOver += GameOver;
        gameObject.SetActive(false);
        boarderDefaultColor = boarderBar.color;
    }

    private void GameOver(Game game)
    {
        if(game.winner == Winner.Pending)
            return;
            
        gameObject.SetActive(true);
        text.color = Color.red;
        boarderBar.color = Color.red;
        hexOutline.color = Color.red;

        BoardState finalState = game.turnHistory[game.turnHistory.Count - 1];
        Team loser = game.winner == Winner.White ? Team.Black : Team.White;

        text.text = game.endType switch {
            GameEndType.Pending => SupportOldSaves(game),
            GameEndType.Draw => "",
            GameEndType.Checkmate => "Checkmate",
            GameEndType.Surrender => $"{loser}\n surrendered",
            GameEndType.Flagfall => $"{loser}\n flagfell",
            GameEndType.Stalemate => "Stalemate",
            _ => ""
        };
    }

    private string SupportOldSaves(Game game)
    {
        Team loser = game.winner == Winner.White ? Team.Black : Team.White;

        if(game.winner == Winner.Pending)
            return "";
        else if(game.winner == Winner.Draw)
            return "Draw";
        else if(game.turnHistory[game.turnHistory.Count - 1].checkmate > Team.None)
            return "Checkmate";
        else
            return $"{loser}\n surrendered";
    }

    private void NewTurn(BoardState newState)
    {
        if(newState.check == Team.None && newState.checkmate == Team.None)
        {
            gameObject.SetActive(false);
            if(boarderBar.color != boarderDefaultColor)
                boarderBar.color = boarderDefaultColor;
            return;
        }
        
        gameObject.SetActive(true);
        if(newState.check > 0)
        {
            text.color = Color.yellow;
            boarderBar.color = Color.yellow;
            hexOutline.color = Color.yellow;
            text.text = "Check";
        }
        else if(newState.checkmate > 0)
        {
            text.color = Color.red;
            boarderBar.color = Color.red;
            hexOutline.color = Color.red;
            text.text = "Checkmate";
        }
        else
            text.text = "";
    }
}