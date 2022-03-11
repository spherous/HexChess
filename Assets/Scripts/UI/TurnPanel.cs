using UnityEngine;
using TMPro;
using System;
using Extensions;

public class TurnPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private GameObject turnTextPanel;
    [SerializeField] private TextMeshProUGUI gameConclusionText;

    Board board;
    Multiplayer multiplayer;

    [SerializeField] private Color orangeColor;

    [SerializeField] private GroupFader whiteIconFader;
    [SerializeField] private GroupFader blackIconFader;

    public float onesWidth;
    public float tensWidth;
    public float hundredsWidth;

    private void Awake() {
        board = GameObject.FindObjectOfType<Board>();
        board.newTurn += NewTurn;
        board.gameOver += GameOver;
        multiplayer = GameObject.FindObjectOfType<Multiplayer>();
        blackIconFader.Disable();
        turnText.rectTransform.sizeDelta = new Vector2(onesWidth, turnText.rectTransform.sizeDelta.y);
    }

    public void GameOver(Game game) => SetGameEndText(game);

    public void SetGameEndText(Game game)
    {
        if(turnTextPanel.activeSelf)
            turnTextPanel.SetActive(false);

        Team loser = game.winner == Winner.White ? Team.Black : Team.White;

        if(game.winner == Winner.White && blackIconFader.visible)
        {
            blackIconFader.FadeOut();
            whiteIconFader.FadeIn();
        }
        else if(game.winner == Winner.Black && whiteIconFader.visible)
        {
            whiteIconFader.FadeOut();
            blackIconFader.FadeIn();
        }
        else if(game.winner == Winner.Draw || game.winner == Winner.None || game.winner == Winner.Pending)
        {
            if(whiteIconFader.visible)
                whiteIconFader.FadeOut();
            if(blackIconFader.visible)
                blackIconFader.FadeOut();
        }

        float gameLength = game.GetGameLength();
        string formattedGameLength = TimeSpan.FromSeconds(gameLength).ToString(gameLength.GetStringFromSeconds());
        int turnCount = game.GetTurnCount();
        string durationString = game.timerDuration == 0 && !game.hasClock 
            ? $"On turn {turnCount}" 
            : $"On turn {turnCount} in {formattedGameLength}";

        string teamColor = game.winner == Winner.White ? "FFFFFF" : game.winner == Winner.Black ? "FF8620" : "939087";

        gameConclusionText.text = game.endType switch {
            // game.endType was added in v1.0.8 to support flagfalls and stalemates, any game saves from before then will default to Pending
            GameEndType.Pending => SupportOldSaves(game, teamColor),
            GameEndType.Stalemate => $"<color=#DB0E0E>Game over!</color>\n {durationString} a stalemate has occured.",
            GameEndType.Draw => $"<color=#DB0E0E>Game over!</color>\n {durationString}, a draw has occured.",
            _ => $"<color=#DB0E0E>Game over!</color>\n {durationString} <color=#{teamColor}>{game.winner}</color> has won!"
        };
    }

    private string SupportOldSaves(Game game, string teamColor)
    {
        // We can figure out most of what we need here, including if it actually is a pending game
        int turnCount = game.GetTurnCount();
        string turnPlurality = turnCount > 1 ? "turns" : "turn";
        return game.winner switch {
            Winner.Pending => "",
            Winner.None => $"<color=#DB0E0E>Game over!</color>\n After {turnCount} {turnPlurality}, a stalemate has occured.",
            Winner.Draw => $"<color=#DB0E0E>Game over!</color>\n After {turnCount} {turnPlurality}, a draw has occured.",
            _ => $"<color=#DB0E0E>Game over!</color>\n After {turnCount} <color=#{teamColor}>{game.winner}</color> has won!"
        };
    }

    public void NewTurn(BoardState newState)
    {
        int turnCount = board.currentGame.GetTurnCount() + board.currentGame.turnHistory.Count % 2;
        NewTurn(newState, turnCount);
    }

    public void NewTurn(BoardState newState, int turnCount)
    {
        if(!turnTextPanel.activeSelf)
            turnTextPanel.SetActive(true);

        gameConclusionText.text = "";

        string text = multiplayer == null 
            ? newState.currentMove == Team.White ? "White's" : "Black's"
            : newState.currentMove == multiplayer.localTeam ? "Your" : "Opponent's";

        float width = turnCount >= 100 ? hundredsWidth : turnCount < 100 && turnCount >= 10 ? tensWidth : onesWidth;        
        turnText.rectTransform.sizeDelta = new Vector2(width, turnText.rectTransform.sizeDelta.y);

        turnText.text = $"{turnCount}:{text}";
        turnText.color = newState.currentMove == Team.White ? Color.white : orangeColor;

        if(newState.currentMove == Team.White && blackIconFader.visible)
        {
            blackIconFader.FadeOut();
            whiteIconFader.FadeIn();
        }
        else if(newState.currentMove != Team.White && whiteIconFader.visible)
        {
            whiteIconFader.FadeOut();
            blackIconFader.FadeIn();
        }
    }
}