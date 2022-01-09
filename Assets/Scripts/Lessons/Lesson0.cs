using System;
using Unity.MLAgents;
using UnityEngine;

public class Lesson0 : MonoBehaviour
{
    public Hexmachina agentPrefab;
    private Hexmachina agent;
    public Board board;

    private void Awake() {
        agent = Instantiate(agentPrefab);
        agent.Init(board, 0);
        board.newTurn += NewTurn;
        board.gameOver += GameOver;
    }

    private void GameOver(Game game) => Reset();

    private void NewTurn(BoardState newState)
    {
        if(newState.currentMove == Team.Black)
        {
            if(newState.checkmate != Team.Black)
            {
                Debug.Log("Failed");
                agent.AddReward(-1);
                Reset();
            }
            else
            {
                Debug.Log("Success");
                agent.AddReward(1);
            }
        }
        else if(newState.currentMove == Team.White)
            agent.RequestDecision();
    }

    private void Reset()
    {
        agent.EndEpisode();
        Destroy(agent.gameObject);
        agent = Instantiate(agentPrefab);
        agent.Init(board, 0);
        board.ResetPieces();
    }
}