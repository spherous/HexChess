﻿using System;
using System.Collections.Generic;

public class EvaluationData
{
    /*
    Terminology:
    Piece: Anything from king to pawn
    Threat: A hex that could be captured if occupied
    Attacks: A hex that can be attacked right now
    Defends: A hex that we occupy but also threaten
    */
    public BitsBoard WhitePieces;
    public BitsBoard BlackPieces;

    public BitsBoard WhitePawns;
    public BitsBoard BlackPawns;

    public BitsBoard WhiteThreats;
    public BitsBoard BlackThreats;
    public BitsBoard WhitePawnThreats;
    public BitsBoard BlackPawnThreats;

    public void Prepare(FastBoardNode node)
    {
        var whitePieces = new BitsBoard();
        var blackPieces = new BitsBoard();

        var whitePawns = new BitsBoard();
        var blackPawns = new BitsBoard();

        var whiteThreats = new BitsBoard();
        var blackThreats = new BitsBoard();

        for (byte b = 0; b < node.positions.Length; ++b)
        {
            var piece = node.positions[b];
            if (piece.team == Team.None)
                continue;

            else if (piece.team == Team.White)
                whitePieces[b] = true;
            else
                blackPieces[b] = true;

            if (piece.piece == FastPiece.Pawn)
            {
                if (piece.team == Team.White)
                    whitePawns[b] = true;
                else
                    blackPawns[b] = true;
            }
            else
            {
                if (piece.team == Team.White)
                    AddThreats(ref whiteThreats, node, b);
                else
                    AddThreats(ref blackThreats, node, b);
            }
        }

        var whitePawnThreats = whitePawns.Shift(HexNeighborDirection.UpLeft) | whitePawns.Shift(HexNeighborDirection.UpRight);
        var blackPawnThreats = blackPawns.Shift(HexNeighborDirection.DownLeft) | blackPawns.Shift(HexNeighborDirection.DownRight);

        WhitePawns = whitePawns;
        BlackPawns = blackPawns;

        WhitePieces = whitePieces;
        BlackPieces = blackPieces;

        WhiteThreats = whiteThreats | whitePawnThreats;
        BlackThreats = blackThreats | blackPawnThreats;

        WhitePawnThreats = whitePawnThreats;
        BlackPawnThreats = blackPawnThreats;
    }

    static void AddThreats(ref BitsBoard threats, FastBoardNode node, byte index)
    {
        var piece = node[index];
        switch (piece.piece)
        {
            case FastPiece.King:
                threats = threats | PrecomputedMoveData.kingThreats[index];
                return;

            case FastPiece.Knight:
                threats = threats | PrecomputedMoveData.knightThreats[index];
                return;

            case FastPiece.Squire:
                threats = threats | PrecomputedMoveData.squireThreats[index];
                return;

            case FastPiece.Bishop:
                AddThreatRays(ref threats, node, PrecomputedMoveData.bishopRays[index]);
                return;
            case FastPiece.Rook:
                AddThreatRays(ref threats, node, PrecomputedMoveData.rookRays[index]);
                return;

            case FastPiece.Queen:
                AddThreatRays(ref threats, node, PrecomputedMoveData.bishopRays[index]);
                AddThreatRays(ref threats, node, PrecomputedMoveData.rookRays[index]);
                return;

            case FastPiece.None:
            case FastPiece.Pawn: // Pawn handled by caller
            default:
                return;
        }
    }

    static void AddThreatRays(ref BitsBoard threats, FastBoardNode node, FastIndex[][] rays)
    {
        foreach (var ray in rays)
        {
            foreach (var move in ray)
            {
                threats[move] = true;
                if (node[move].team != Team.None)
                    break;
            }
        }
    }
}
