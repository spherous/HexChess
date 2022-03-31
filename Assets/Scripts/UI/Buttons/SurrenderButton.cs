using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SurrenderButton : TwigglyButton
{
    Board board;
    private new void Awake() {
        base.Awake();

        board = GameObject.FindObjectOfType<Board>();
        onClick += Surrender;
        board.gameOver += GameOver;
    }

    private void Surrender()
    {
        Networker networker = GameObject.FindObjectOfType<Networker>();
        float timestamp = board.currentGame.CurrentTime;
        if (networker != null)
        {
            networker.SendMessage(new Message(
                type: MessageType.Surrender,
                data: Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(timestamp))
            ));
            Team surrenderingTeam = networker.isHost ? networker.host.team : networker.player.Value.team;
            board.currentGame.Surrender(surrenderingTeam);
        }
        else
            board.currentGame.Surrender(board.GetCurrentTurn());
    }

    private void GameOver(Game game)
    {
        board.gameOver -= GameOver;
        onClick -= Surrender;

        Destroy(gameObject);
    }
}