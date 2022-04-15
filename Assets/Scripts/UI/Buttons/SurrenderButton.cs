using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public class SurrenderButton : TwigglyButton
{
    Board board;
    private new void Awake() {
        base.Awake();

        board = GameObject.FindObjectOfType<Board>();
        onClick += Surrender;
        board.gameOver += GameOver;
        board.newTurn += NewTurn;
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

    private void NewTurn(BoardState newState)
    {
        if(!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            onClick += Surrender;
        }
    }

    private void GameOver(Game game)
    {
        onClick -= Surrender;

        gameObject.SetActive(false);
    }
}