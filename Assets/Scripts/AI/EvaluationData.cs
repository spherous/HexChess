using System;
using System.Collections.Generic;

public class EvaluationData
{
    public class BoardCollection
    {
        public BitsBoard Pieces;
        public BitsBoard Pawns;
        public BitsBoard Threats;
        public BitsBoard PawnThreats;
        public int MaterialValue;

        public void Clear()
        {
            Pieces = default;
            Pawns = default;
            Threats = default;
            PawnThreats = default;
            MaterialValue = 0;
        }
    }

    /*
    Terminology:
    Piece: Anything from king to pawn
    Threat: A hex that could be captured if occupied
    Attacks: A hex that can be attacked right now
    Defends: A hex that we occupy but also threaten
    */

    public BoardCollection White = new BoardCollection();
    public BoardCollection Black = new BoardCollection();

    public void Prepare(FastBoardNode node)
    {
        White.Clear();
        Black.Clear();

        for (byte b = 0; b < node.positions.Length; ++b)
        {
            var piece = node.positions[b];
            BoardCollection board;
            if (piece.team == Team.None)
                continue;
            else if (piece.team == Team.White)
                board = White;
            else
                board = Black;

            board.Pieces[b] = true;
            board.MaterialValue += GetMaterialValue(piece.piece);

            if (piece.piece == FastPiece.Pawn)
            {
                board.Pawns[b] = true;
            }
            else
            {
                AddThreats(ref board.Threats, node, b);
            }
        }

        White.PawnThreats = White.Pawns.Shift(HexNeighborDirection.UpLeft) | White.Pawns.Shift(HexNeighborDirection.UpRight);
        Black.PawnThreats = Black.Pawns.Shift(HexNeighborDirection.DownLeft) | Black.Pawns.Shift(HexNeighborDirection.DownRight);

        White.Threats = White.Threats | White.PawnThreats;
        Black.Threats = Black.Threats | Black.PawnThreats;
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
    static int GetMaterialValue(FastPiece piece)
    {
        switch (piece)
        {
            case FastPiece.King:
                return 0;

            case FastPiece.Queen:
                return 1000;

            case FastPiece.Rook:
                return 525;

            case FastPiece.Knight:
                return 350;

            case FastPiece.Bishop:
                return 350;

            case FastPiece.Squire:
                return 300;

            case FastPiece.Pawn:
            default:
                return 100;
        }
    }
}
