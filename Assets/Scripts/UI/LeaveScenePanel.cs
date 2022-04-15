using UnityEngine;

public class LeaveScenePanel : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private GroupFader fader;

    private void Awake() {
        board.gameOver += GameOver;
        board.newTurn += NewTurn;
    }

    private void NewTurn(BoardState newState)
    {
        if(fader.visible)
            fader.FadeOut();
    }

    private void GameOver(Game game) => fader?.FadeIn();
}