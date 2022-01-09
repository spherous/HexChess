using System.Collections.Generic;
using UnityEngine;

public class Lesson8 : MonoBehaviour
{
    public Hexmachina agentPrefab;
    private Hexmachina agent;
    public Board board;

    Team turn = Team.None;

    private void Awake() {
        agent = Instantiate(agentPrefab);
        agent.Init(board, 0);
        board.newTurn += NewTurn;
        board.gameOver += GameOver;
    }

    private void FixedUpdate() {
        if(turn == Team.White)
        {
            turn = Team.None;
            agent.RequestDecision();
        }
        else if(turn == Team.Black)
        {
            // Dumb random AI
            turn = Team.None;
            BoardState state = board.GetCurrentBoardState();
            List<(Piece piece, Index target, MoveType moveType)> moves = board.currentGame.GetAllValidMovesForTeam(Team.Black, state);
            (Piece piece, Index target, MoveType moveType) move = moves[UnityEngine.Random.Range(0, moves.Count)];
            IPiece piece = board.activePieces[(Team.Black, move.piece)];


            var newStateWithPromos = board.currentGame.QueryMove(piece.location, (move.target, move.moveType), state, Piece.Queen);
            if((piece is Pawn pawn) && pawn.GetGoalInRow(move.target.row) == move.target.row)
            {
                board.currentGame.SetPromotions(newStateWithPromos.promotions);
                piece = board.PromoteIPiece(pawn, Piece.Queen);
            }
            board.AdvanceTurn(newStateWithPromos.newState, true, true);
        }
    }

    private void NewTurn(BoardState newState)
    {
        Move move = board.currentGame.GetLastMove();
        float reward = move.capturedPiece.HasValue ? 0.001f : 0f;
        int mod = newState.currentMove == Team.White ? 1 : newState.currentMove == Team.Black ? -1 : 0;
        
        agent.AddReward(reward * mod);
        
        turn = newState.currentMove;
    }

    private void GameOver(Game game)
    {
        int reward = game.winner switch
        {
            Winner.White => 1,
            Winner.Black => -1,
            _ => 0
        };
        agent.AddReward(reward);

        Reset();     
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