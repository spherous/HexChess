using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Extensions;
using static Extensions.Extensions;
using System;

public class Hexmachina : Agent
{
    [SerializeField] private Board board;
    [SerializeField] private BehaviorParameters parameters;
    List<(IPiece, Index, MoveType)> cachedMoves = new List<(IPiece, Index, MoveType)>();
    public int vectorObservationSize => parameters.BrainParameters.VectorObservationSize;

    public void Init(Board board, int teamID)
    {
        this.board = board;
        parameters.TeamId = teamID;
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if(cachedMoves.Count != 694)
            Debug.LogError($"Cached moves is of size: {cachedMoves.Count}");

        for(int i = 0; i < cachedMoves.Count; i++)
        {
            (IPiece piece, Index index, MoveType type) move = cachedMoves[i];
            if(move.type == MoveType.None)
            {
                actionMask.SetActionEnabled(0, i, false);
                continue;
            }
            actionMask.SetActionEnabled(0, i, true);
        }
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        BoardState state = board.GetCurrentBoardState();
        List<Promotion> promotions = board.currentGame.promotions;
        cachedMoves.Clear();

        // current turn
        // sensor.AddObservation((float)state.currentMove);
        // progression towards 50 move rule
        // sensor.AddObservation(board.turnsSincePawnMovedOrPieceTaken);

        // Observe the boardstate
        // foreach(Index index in Index.GetAllIndices())
        // {
        //     if(state.TryGetPiece(index, out (Team team, Piece piece) teamedPiece))
        //     {
        //         IEnumerable<Promotion> applicablePromos = promotions.Where(promo => promo.from == teamedPiece.piece && promo.team == teamedPiece.team);
        //         Piece realPiece = applicablePromos.Any() ? applicablePromos.First().to : teamedPiece.piece;
                
        //         sensor.AddObservation(new Vector3(
        //             (float)teamedPiece.team,
        //             (float)realPiece,
        //             index.GetSingleVal()
        //         ));
        //     }
        //     else
        //         sensor.AddObservation(new Vector3(-1, -1, index.GetSingleVal()));
        // }

        // Observe all potential moves
        foreach(Piece piece in EnumArray<Piece>.Values)
        {
            int pieceMaxMoves = piece.GetMaxMoveCount();
            IPiece pieceObj = board.activePieces.ContainsKey((state.currentMove, piece)) ? board.activePieces[(state.currentMove, piece)] : null;
            if(pieceObj != null)
            {
                Piece realPiece = piece;
                if(piece.IsPawn() && !(pieceObj is Pawn))
                    realPiece = board.currentGame.GetRealPiece((state.currentMove, piece));

                // This needs to have all moves that should not be allowed to be played to be turned to invalid moves
                IEnumerable<(Index index, MoveType type)> allMoves = MoveGenerator.GetAllMovesIncludingInvalid(pieceObj.location, realPiece, state.currentMove, state, board.currentGame.promotions);
                // allMoves = board.InvalidateImpossibleMoves(allMoves, pieceObj, state);

                foreach((Index index, MoveType type) move in allMoves)
                {
                    int from = pieceObj.location.GetSingleVal();
                    int to = move.index.GetSingleVal();
                    cachedMoves.Add((pieceObj, move.index, move.type));
                    sensor.AddObservation(new Vector3(from, to, (int)move.type));
                }

                // Pad pawn or non-queen promoted pawn
                if(piece.IsPawn() && realPiece != Piece.Queen)
                {
                    int padAmound = 58 - realPiece.GetMaxMoveCount();
                    for(int i = 0; i < padAmound; i++)
                        ObserveInvalidMove(sensor, pieceObj);
                }
            }
            else
            {
                if(piece.IsPawn())
                {
                    for(int i = 0; i < Piece.Queen.GetMaxMoveCount(); i++)
                        ObserveInvalidMove(sensor);
                }
                else
                {
                    for(int i = 0; i < pieceMaxMoves; i++)
                        ObserveInvalidMove(sensor);
                }
            }
        }
    }

    private void ObserveInvalidMove(VectorSensor sensor, IPiece piece = null)
    {
        cachedMoves.Add((piece, Index.invalid, MoveType.None));
        sensor.AddObservation(invalidMove);
    }

    public override void OnActionReceived(ActionBuffers action)
    {
        // Debug.Log($"choice: {(int)vectorAction[0]}, max: {cachedMoves.Count}");
        MakeDecision(action.DiscreteActions[0], action.DiscreteActions[1]);
    }
    public override void Heuristic(in ActionBuffers actions)
    {
        MakeDecision(actions.DiscreteActions[0], actions.DiscreteActions[1]);
    }

    private void MakeDecision(int index, int promoChoice)
    {
        if(index < 0 || index > cachedMoves.Count - 1)
        {
            RequestDecision();
            return;
        }

        (IPiece piece, Index target, MoveType type) move = cachedMoves[index];
        // Debug.Log($"Playing {move.piece.team}:{move.piece.piece} from {move.piece.location.GetKey()} to {move.target.GetKey()}");

        // AI Played a promo
        if((move.piece is Pawn pawn) && pawn.GetGoalInRow(move.target.row) == move.target.row)
        {
            Debug.Log($"Promo choice: {promoChoice}");
            Piece promoTo = GetPromoPiece(promoChoice);
            move.piece = board.PromoteIPiece(pawn, promoTo);
            AddReward(0.0001f);
        }

        // Query move
        // BoardState queryState = board.QueryMove(move, currentBoardState);

        // Punish for progressing 5 fold repetition
        // int fiveFoldProgres = board.GetFiveFoldProgress(queryState);
        // if(board.CheckFiveFoldProgress(queryState))
        //     AddReward(-0.5f - (0.01f * fiveFoldProgres));

        // Play move
        var newStateWithPromos = board.currentGame.QueryMove(move.piece.location, (move.target, move.type), board.GetCurrentBoardState(), Piece.Queen);
        board.currentGame.SetPromotions(newStateWithPromos.promotions);
        // currentBoardState = board.ExecuteMove(move, currentBoardState);
        board.AdvanceTurn(newStateWithPromos.newState, true, true);
        
        // Move m = BoardState.GetLastMove(board.turnHistory);
        // AddReward(-0.00001f * m.turn);

        AddReward(-0.00005f);
    }

    public Piece GetPromoPiece(int promo) => promo switch {
        0 => Piece.KingsRook,
        1 => Piece.KingsKnight,
        2 => Piece.BlackSquire,
        3 => Piece.KingsBishop,
        4 => Piece.Queen,
        _ => Piece.Pawn1
    };
}