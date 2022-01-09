using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Extensions;

public class HexmachinaManager : MonoBehaviour
{
    [SerializeField] private Hexmachina agentPrefab;
    [SerializeField] private Board board;
    public bool whiteAI = false;
    public bool blackAI = true;
    public Hexmachina whiteAgent;
    public Hexmachina blackAgent;

    private bool gameOver = false;
    private Hexmachina nextDecision;
    bool firstDecision = true;

    float orgFixedTimestep;
    float fixedTimestep = 0.1f;

    private void Start() {
        orgFixedTimestep = Time.fixedDeltaTime;
        Time.fixedDeltaTime = fixedTimestep;

        board.newTurn += NewTurn;
        board.gameOver += GameOver;

        SpawnAI();

        if(whiteAI)
            nextDecision = whiteAgent;
    }

    private void OnDestroy() => Time.fixedDeltaTime = orgFixedTimestep;

    private void FixedUpdate()
    {
        if(firstDecision)
        {
            firstDecision = false;
            NewTurn(board.GetCurrentBoardState());
            return;
        }

        nextDecision?.RequestDecision();
    }

    public void GameOver(Game game)
    {
        switch(game.winner)
        {
            case Winner.White:
                if(whiteAI)
                    whiteAgent?.AddReward(1);
                if(blackAI)
                    blackAgent?.AddReward(-1);
                break;
            case Winner.Black:
                if(whiteAI)
                    whiteAgent?.AddReward(-1);
                if(blackAI)
                    blackAgent?.AddReward(1);
                break;
            case Winner.Draw:
                if(whiteAI)
                    whiteAgent?.AddReward(0);
                if(blackAI)
                    blackAgent?.AddReward(0);
                break;
            case Winner.None:
                if(whiteAI)
                    whiteAgent?.AddReward(0);
                if(blackAI)
                    blackAgent?.AddReward(0);
                break;
        }

        if(whiteAI && whiteAgent != null)
        {
            whiteAgent.EndEpisode();
            Destroy(whiteAgent.gameObject);
        }
        if(blackAI && blackAgent != null)
        {
            blackAgent.EndEpisode();
            Destroy(blackAgent.gameObject);
        }

        gameOver = true;
        // Reset();
    }

    private void SpawnAI()
    {
        if(whiteAI)
        {
            if(whiteAgent != null)
                Destroy(whiteAgent.gameObject);
            whiteAgent = Instantiate(agentPrefab);
            whiteAgent.Init(board, 0);
        }
        if(blackAI)
        {
            if(blackAgent != null)
                Destroy(blackAgent.gameObject);
            blackAgent = Instantiate(agentPrefab);
            blackAgent.Init(board, 1);
        }
    }

    private void Reset()
    {
        board.ResetPieces();
        // board.LoadGame(board.GetGame(board.gameToLoadLoc));
        SpawnAI();
        
        gameOver = false;
        NewTurn(board.GetCurrentBoardState());
    }

    private void NewTurn(BoardState newState)
    {
        if(gameOver)
            return;

        Move m = board.currentGame.GetLastMove();
        if(m.capturedPiece.HasValue)
        {
            // Reward the team that captured a piece, while punishing the team who's piece was captured
            // Queen .9
            // Rook .5
            // Bishop/Knight .3
            // Squire .2
            // Pawn .1
            // float val = 0.1f * (float)m.capturedPiece.Value.GetMaterialValue();
            float val = 0.1f;

            Hexmachina positiveAgent = m.lastTeam == Team.White ? whiteAI ? whiteAgent : null : m.lastTeam == Team.Black ? blackAI ? blackAgent : null : null;
            Hexmachina negativeAgent = m.lastTeam == Team.White ? blackAI ? blackAgent : null : m.lastTeam == Team.Black ? whiteAI ? whiteAgent : null : null;

            positiveAgent?.AddReward(val);
            negativeAgent?.AddReward(-val);
        }
        
        // Reward the team that checked or mated, but punish the team that was checked or mated
        // if(newState.checkmate != Team.None){ }
        // else if(newState.check != Team.None)
        // {
        //     if(newState.check == Team.White)
        //     {
        //         if(whiteAI)
        //             whiteAgent?.AddReward(-0.2f);
        //         if(blackAI)
        //             blackAgent?.AddReward(0.2f);
        //     }
        //     else
        //     {
        //         if(whiteAI)
        //             whiteAgent?.AddReward(0.2f);
        //         if(blackAI)
        //             blackAgent?.AddReward(-0.2f);
        //     }
        // }

        Hexmachina agent = newState.currentMove switch {
            Team.White when whiteAI => whiteAgent,
            Team.Black when blackAI => blackAgent,
            _ => null
        };

        // agent?.RequestDecision();
        nextDecision = agent;
    }
}