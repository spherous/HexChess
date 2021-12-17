using System;
using System.Collections.Generic;
using Extensions;

public static class FastPossibleMoveGenerator
{
    public static bool newstuff;

    public static readonly FastPiece[] DefendableTypes = new FastPiece[]
    {
        FastPiece.King,
        FastPiece.Queen,
        FastPiece.Knight,
        FastPiece.Bishop,
        FastPiece.Squire,
    };

    public static void AddAllPossibleMoves(List<FastMove> moves, FastIndex start, FastPiece piece, Team team, FastBoardNode boardNode, bool generateQuiet = true)
    {
        switch (piece)
        {
            case FastPiece.King:
                AddAllPossibleKingMoves(moves, start, team, boardNode, generateQuiet);
                return;
            case FastPiece.Queen:
                AddAllPossibleQueenMoves(moves, start, team, boardNode, generateQuiet);
                return;
            case FastPiece.Rook:
                AddAllPossibleRookMoves(moves, start, team, boardNode, generateQuiet);
                return;
            case FastPiece.Knight:
                AddAllPossibleKnightMoves(moves, start, team, boardNode, generateQuiet);
                return;
            case FastPiece.Bishop:
                AddAllPossibleBishopMoves(moves, start, team, boardNode, generateQuiet);
                return;
            case FastPiece.Squire:
                AddAllPossibleSquireMoves(moves, start, team, boardNode, generateQuiet);
                return;
            case FastPiece.Pawn:
                AddAllPossiblePawnMoves(moves, start, team, boardNode, generateQuiet);
                return;
            default:
                throw new ArgumentException($"Unhandled piece type: {piece}", nameof(piece));
        }
    }

    public static void AddAllPossibleKingMoves(List<FastMove> moves, FastIndex start, Team team, FastBoardNode boardNode, bool generateQuiet = true)
    {
        var validMoves = PrecomputedMoveData.kingMoves[start.ToByte()];
        foreach (var target in validMoves)
        {
            if (boardNode.TryGetPiece(target, out (Team team, FastPiece piece) occupier))
            {
                if (occupier.team != team)
                    moves.Add(new FastMove(start, target, MoveType.Attack));
            }
            else if (generateQuiet)
                moves.Add(new FastMove(start, target, MoveType.Move));
        }
    }

    public static void AddAllPossibleSquireMoves(List<FastMove> moves, FastIndex start, Team team, FastBoardNode boardNode, bool generateQuiet = true)
    {
        foreach (var target in PrecomputedMoveData.squireMoves[start.ToByte()])
        {
            if (boardNode.TryGetPiece(target, out (Team team, FastPiece piece) occupier))
            {
                if (occupier.team != team)
                    moves.Add(new FastMove(start, target, MoveType.Attack));
            }
            else if (generateQuiet)
                moves.Add(new FastMove(start, target, MoveType.Move));
        }
    }

    public static void AddAllPossibleKnightMoves(List<FastMove> moves, FastIndex start, Team team, FastBoardNode boardNode, bool generateQuiet = true)
    {
        foreach (var target in PrecomputedMoveData.knightMoves[start.ToByte()])
        {
            if (boardNode.TryGetPiece(target, out (Team team, FastPiece piece) occupier))
            {
                if (occupier.team != team)
                    moves.Add(new FastMove(start, target, MoveType.Attack));
            }
            else if (generateQuiet)
                moves.Add(new FastMove(start, target, MoveType.Move));
        }
    }

    public static void AddAllPossibleBishopMoves(List<FastMove> moves, FastIndex start, Team team, FastBoardNode boardNode, bool generateQuiet = true)
    {
        FastIndex[][] bishopRays = PrecomputedMoveData.bishopRays[start.ToByte()];
        foreach (var ray in bishopRays)
        {
            AddRayMoves(moves, start, team, boardNode, ray, generateQuiet);
        }
    }

    public static void AddAllPossibleRookDirectionalMoves(List<FastMove> moves, FastIndex start, Team team, FastBoardNode boardNode, bool generateQuiet = true)
    {
        FastIndex[][] rookRays = PrecomputedMoveData.rookRays[start.ToByte()];
        foreach (var directionalIndexes in rookRays)
        {
            AddRayMoves(moves, start, team, boardNode, directionalIndexes, generateQuiet);
        }
    }

    static void AddRayMoves(List<FastMove> moves, FastIndex start, Team team, FastBoardNode boardNode, FastIndex[] ray, bool generateQuiet = true)
    {
        foreach (var target in ray)
        {
            var occupant = boardNode[target];
            if (occupant.team == Team.None)
            {
                if (generateQuiet)
                    moves.Add(new FastMove(start, target, MoveType.Move)); // empty
            }
            else
            {
                if (occupant.team != team)
                    moves.Add(new FastMove(start, target, MoveType.Attack)); // rip and tear
                break;
            }
        }
    }

    public static void AddAllPossibleRookMoves(List<FastMove> moves, FastIndex start, Team team, FastBoardNode boardNode, bool generateQuiet = true)
    {
        AddAllPossibleRookDirectionalMoves(moves, start, team, boardNode, generateQuiet);

        if (generateQuiet)
        {
            foreach (var target in PrecomputedMoveData.kingMoves[start.ToByte()])
            {
                if (boardNode.TryGetPiece(target, out (Team team, FastPiece piece) occupier))
                {
                    if (occupier.team == team && Contains(DefendableTypes, occupier.piece))
                        moves.Add(new FastMove(start, target, MoveType.Defend));
                }
            }
        }
    }

    public static void AddAllPossibleQueenMoves(List<FastMove> moves, FastIndex start, Team team, FastBoardNode boardNode, bool generateQuiet = true)
    {
        AddAllPossibleRookDirectionalMoves(moves, start, team, boardNode, generateQuiet);
        AddAllPossibleBishopMoves(moves, start, team, boardNode, generateQuiet);
    }

    #region Pawn
    public static void AddAllPossiblePawnMoves(List<FastMove> moves, FastIndex start, Team team, FastBoardNode boardNode, bool generateQuiet = true)
    {
        Team enemy = team.Enemy();

        HexNeighborDirection leftAttackDir = HexNeighborDirection.UpLeft;
        HexNeighborDirection leftPassantDir = HexNeighborDirection.DownLeft;
        HexNeighborDirection rightAttackDir = HexNeighborDirection.UpRight;
        HexNeighborDirection rightPassantDir = HexNeighborDirection.DownRight;
        HexNeighborDirection forwardDir = HexNeighborDirection.Up;
        if (team != Team.White)
        {
            leftAttackDir = HexNeighborDirection.DownLeft;
            leftPassantDir = HexNeighborDirection.UpLeft;
            rightAttackDir = HexNeighborDirection.DownRight;
            rightPassantDir = HexNeighborDirection.UpRight;
            forwardDir = HexNeighborDirection.Down;
        }

        if (start.TryGetNeighbor(leftAttackDir, out var leftAttack))
        {
            var leftVictim = boardNode[leftAttack];
            if (leftVictim.team == enemy)
            {
                moves.Add(new FastMove(start, leftAttack, MoveType.Attack));
            }
            if (leftVictim.team == Team.None)
            {
                if (start.TryGetNeighbor(leftPassantDir, out var leftPassant) && boardNode.passantableIndex == leftPassant)
                {
                    moves.Add(new FastMove(start, leftAttack, MoveType.EnPassant));
                }
            }
        }

        if (start.TryGetNeighbor(rightAttackDir, out var rightAttack))
        {
            var rightVictim = boardNode[rightAttack];
            if (rightVictim.team == enemy)
            {
                moves.Add(new FastMove(start, rightAttack, MoveType.Attack));
            }
            if (rightVictim.team == Team.None)
            {
                if (start.TryGetNeighbor(rightPassantDir, out var rightPassant) && boardNode.passantableIndex == rightPassant)
                {
                    moves.Add(new FastMove(start, rightAttack, MoveType.EnPassant));
                }
            }
        }

        if (!generateQuiet)
            return;

        // One forward
        bool hasForward = start.TryGetNeighbor(forwardDir, out var forward);
        if (hasForward && !boardNode.IsOccupied(forward))
        {
            if (forward.TryGetNeighbor(forwardDir, out var twoForward))
            {
                moves.Add(new FastMove(start, forward, MoveType.Move));

                // Two forward on 1st move
                if (PawnIsAtStart(team, (Index)start) && !boardNode.IsOccupied(twoForward))
                    moves.Add(new FastMove(start, twoForward, MoveType.Move));
            }
            else
            {
                // if two squares forward doesn't exist, that means we're on the last rank for this pawn.
                moves.Add(new FastMove(start, forward, MoveType.Move, FastPiece.Queen));
                moves.Add(new FastMove(start, forward, MoveType.Move, FastPiece.Rook));
                moves.Add(new FastMove(start, forward, MoveType.Move, FastPiece.Knight));
                moves.Add(new FastMove(start, forward, MoveType.Move, FastPiece.Squire));
            }
        }
    }

    private static bool PawnIsAtStart(Team team, Index location)
    {
        if (team == Team.White)
        {
            return location.row == 3 || location.row == 4;
        }
        else
        {
            return location.row == 14 || location.row == 15;
        }
    }

    #endregion

    private static bool Contains(FastPiece[] haystack, FastPiece needle)
    {
        return Array.IndexOf(haystack, needle) >= 0;
    }
}
