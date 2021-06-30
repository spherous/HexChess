using System;
using System.Collections.Generic;
using Extensions;

public static class MoveGenerator
{
    public static bool IsPromotionRank(Team team, Index target)
    {
        HexNeighborDirection forward = team == Team.White ? HexNeighborDirection.Up : HexNeighborDirection.Down;
        return !target.TryGetNeighbor(forward, out Index _);
    }

    public static readonly Piece[] DefendableTypes = new Piece[]
    {
        Piece.King,
        Piece.Queen,
        Piece.KingsKnight,
        Piece.QueensKnight,
        Piece.KingsBishop,
        Piece.QueensBishop,
        Piece.WhiteSquire,
        Piece.GraySquire,
        Piece.BlackSquire,
    };

    public static IEnumerable<(Index, MoveType)> GetAllPossibleMoves(Index location, Piece piece, Team team, BoardState boardState, bool includeBlocking = false)
    {
        switch (piece)
        {
            case Piece.King:
                return GetAllPossibleKingMoves(location, team, boardState, includeBlocking);
            case Piece.Queen:
                return GetAllPossibleQueenMoves(location, team, boardState, includeBlocking);
            case Piece.KingsRook:
            case Piece.QueensRook:
                return GetAllPossibleRookMoves(location, team, boardState, includeBlocking);
            case Piece.KingsKnight:
            case Piece.QueensKnight:
                return GetAllPossibleKnightMoves(location, team, boardState, includeBlocking);
            case Piece.KingsBishop:
            case Piece.QueensBishop:
                return GetAllPossibleBishopMoves(location, team, boardState, includeBlocking);
            case Piece.WhiteSquire:
            case Piece.GraySquire:
            case Piece.BlackSquire:
                return GetAllPossibleSquireMoves(location, team, boardState, includeBlocking);
            case Piece.Pawn1:
            case Piece.Pawn2:
            case Piece.Pawn3:
            case Piece.Pawn4:
            case Piece.Pawn5:
            case Piece.Pawn6:
            case Piece.Pawn7:
            case Piece.Pawn8:
                return GetAllPossiblePawnMoves(location, team, boardState, includeBlocking);
            default:
                throw new ArgumentException($"Unhandled piece type: {piece}", nameof(piece));
        }
    }

    public static IEnumerable<(Index, MoveType)> GetAllPossibleKingMoves(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        foreach(HexNeighborDirection dir in EnumArray<HexNeighborDirection>.Values)
        {
            Index? maybeIndex = location.GetNeighborAt(dir);
            if(maybeIndex == null)
                continue;

            Index index = maybeIndex.Value;

            if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
            {
                if(includeBlocking || occupier.team != team)
                {
                    yield return (index, MoveType.Attack);
                    continue;
                }
                else
                    continue;
            }
            yield return (index, MoveType.Move);
        }
    }

    public static IEnumerable<(Index, MoveType)> GetAllPossibleSquireMoves(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        int squireOffset = location.row % 2 == 0 ? 1 : -1;
        var possible = new (int row, int col)[] {
            (location.row + 3, location.col + squireOffset),
            (location.row - 3, location.col + squireOffset),
            (location.row + 3, location.col),
            (location.row - 3, location.col),
            (location.row, location.col + 1),
            (location.row, location.col - 1)
        };

        foreach((int row, int col) in possible)
        {
            Index index = new Index(row, col);
            if(!index.IsInBounds)
                continue;

            if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
            {
                if (occupier.team == team && !includeBlocking)
                    continue;

                yield return (index, MoveType.Attack);
            }
            else
                yield return (index, MoveType.Move);
        }
    }

    public static IEnumerable<(Index, MoveType)> GetAllPossibleKnightMoves(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        int offset = location.row % 2 == 0 ? 1 : -1;
        var possibleMoves = new (int row, int col)[] {
            (location.row + 5, location.col),
            (location.row + 5, location.col + offset),
            (location.row + 4, location.col + offset),
            (location.row + 4, location.col - offset),
            (location.row + 1, location.col + (2 * offset)),
            (location.row + 1, location.col - offset),
            (location.row - 1, location.col - offset),
            (location.row - 1, location.col + (2 * offset)),
            (location.row - 4, location.col - offset),
            (location.row - 4, location.col + offset),
            (location.row - 5, location.col + offset),
            (location.row - 5, location.col),
        };

        foreach ((int row, int col) in possibleMoves)
        {
            Index index = new Index(row, col);
            if (!index.IsInBounds)
                continue;

            if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
            {
                if(includeBlocking || occupier.team != team)
                    yield return (index, MoveType.Attack);
            }
            else
            {
                yield return(index, MoveType.Move);
            }
        }
    }

    public static IEnumerable<(Index, MoveType)> GetAllPossibleBishopMoves(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        List<(Index, MoveType)> possible = new List<(Index, MoveType)>();
        int offset = location.row % 2;

        // Top Left
        for(
            (int row, int col, int i) = (location.row + 1, location.col - offset, 0);
            row <= Index.rows && col >= 0;
            row++, i++
        ){
            if(!RayCanMove(team, boardState, row, col, possible, includeBlocking))
                break;

            if(i % 2 == offset)
                col--;
        }
        // Top Right
        for(
            (int row, int col, int i) = (location.row + 1, location.col + Math.Abs(1 - offset), 0);
            row <= Index.rows && col <= Index.cols;
            row++, i++
        ){
            if(!RayCanMove(team, boardState, row, col, possible, includeBlocking))
                break;

            if(i % 2 != offset)
                col++;
        }
        // Bottom Left
        for(
            (int row, int col, int i) = (location.row - 1, location.col - offset, 0);
            row >= 0 && col >= 0;
            row--, i++
        ){
            if(!RayCanMove(team, boardState, row, col, possible, includeBlocking))
                break;

            if(i % 2 == offset)
                col--;
        }
        // Bottom Right
        for(
            (int row, int col, int i) = (location.row - 1, location.col + Math.Abs(1 - offset), 0);
            row >= 0 && col <= Index.cols;
            row--, i++
        ){
            if(!RayCanMove(team, boardState, row, col, possible, includeBlocking))
                break;

            if(i % 2 != offset)
                col++;
        }

        return possible;
    }

    public static IEnumerable<(Index, MoveType)> GetAllPossibleRookMoves(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        List<(Index, MoveType)> possible = new List<(Index, MoveType)>();

        // Up
        for(int row = location.row + 2; row <= Index.rows; row += 2)
            if(!RayCanMove(team, boardState, row, location.col, possible, includeBlocking))
                break;
        // Down
        for(int row = location.row - 2; row >= 0; row -= 2)
            if(!RayCanMove(team, boardState, row, location.col, possible, includeBlocking))
                break;
        // Left
        for(int col = location.col - 1; col >= 0; col--)
            if(!RayCanMove(team, boardState, location.row, col, possible, includeBlocking))
                break;
        // Right
        for(int col = location.col + 1; col <= Index.cols - 2 + location.row % 2; col++)
            if(!RayCanMove(team, boardState, location.row, col, possible, includeBlocking))
                break;

        // Check defend
        foreach(HexNeighborDirection dir in EnumArray<HexNeighborDirection>.Values)
        {
            Index? maybeIndex = location.GetNeighborAt(dir);
            if (maybeIndex == null)
                continue;
            Index index = maybeIndex.Value;

            if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
            {
                if(occupier.team == team && Contains(DefendableTypes, occupier.piece))
                    possible.Add((index, MoveType.Defend));
            }
        }

        return possible;
    }

    public static IEnumerable<(Index, MoveType)> GetAllPossibleQueenMoves(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        List<(Index, MoveType)> possible = new List<(Index, MoveType)>();
        int offset = location.row % 2;

        // Up
        for(int row = location.row + 2; row <= Index.rows; row += 2)
            if(!RayCanMove(team, boardState, row, location.col, possible, includeBlocking))
                break;
        // Down
        for(int row = location.row - 2; row >= 0; row -= 2)
            if(!RayCanMove(team, boardState, row, location.col, possible, includeBlocking))
                break;
        // Left
        for(int col = location.col - 1; col >= 0; col--)
            if(!RayCanMove(team, boardState, location.row, col, possible, includeBlocking))
                break;
        // Right
        for(int col = location.col + 1; col <= Index.cols - 2 + location.row % 2; col++)
            if(!RayCanMove(team, boardState, location.row, col, possible, includeBlocking))
                break;

        // Top Left
        for(
            (int row, int col, int i) = (location.row + 1, location.col - offset, 0);
            row <= Index.rows && col >= 0;
            row++, i++
        ){
            if(!RayCanMove(team, boardState, row, col, possible, includeBlocking))
                break;

            if(i % 2 == offset)
                col--;
        }
        // Top Right
        for(
            (int row, int col, int i) = (location.row + 1, location.col + Math.Abs(1 - offset), 0);
            row <= Index.rows && col <= Index.cols;
            row++, i++
        ){
            if(!RayCanMove(team, boardState, row, col, possible, includeBlocking))
                break;

            if(i % 2 != offset)
                col++;
        }
        // Bottom Left
        for(
            (int row, int col, int i) = (location.row - 1, location.col - offset, 0);
            row >= 0 && col >= 0;
            row--, i++
        ){
            if(!RayCanMove(team, boardState, row, col, possible, includeBlocking))
                break;

            if(i % 2 == offset)
                col--;
        }
        // Bottom Right
        for(
            (int row, int col, int i) = (location.row - 1, location.col + Math.Abs(1 - offset), 0);
            row >= 0 && col <= Index.cols;
            row--, i++
        ){
            if(!RayCanMove(team, boardState, row, col, possible, includeBlocking))
                break;

            if(i % 2 != offset)
                col++;
        }

        return possible;
    }

    #region Pawn
    public static IEnumerable<(Index, MoveType)> GetAllPossiblePawnMoves(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        bool isWhite = team == Team.White;
        Index? leftAttack = location.GetNeighborAt(isWhite ? HexNeighborDirection.UpLeft : HexNeighborDirection.DownLeft);
        Index? rightAttack = location.GetNeighborAt(isWhite ? HexNeighborDirection.UpRight : HexNeighborDirection.DownRight);

        // Check takes
        if (leftAttack.HasValue && PawnCanTake(team, leftAttack.Value, boardState, includeBlocking))
            yield return (leftAttack.Value, MoveType.Attack);
        if (rightAttack.HasValue && PawnCanTake(team, rightAttack.Value, boardState, includeBlocking))
            yield return (rightAttack.Value, MoveType.Attack);

        // Check en passant
        Index? leftPassant = location.GetNeighborAt(isWhite ? HexNeighborDirection.DownLeft : HexNeighborDirection.UpLeft);
        Index? rightPassant = location.GetNeighborAt(isWhite ? HexNeighborDirection.DownRight : HexNeighborDirection.UpRight);
        if(leftPassant.HasValue && PawnCanPassant(team, leftPassant.Value, boardState))
            yield return (leftAttack.Value, MoveType.EnPassant);
        if(rightPassant.HasValue && PawnCanPassant(team, rightPassant.Value, boardState))
            yield return (rightAttack.Value, MoveType.EnPassant);

        // One forward
        Index? forward = location.GetNeighborAt(isWhite ? HexNeighborDirection.Up : HexNeighborDirection.Down);
        if (forward.HasValue && !boardState.IsOccupied(forward.Value))
        {
            yield return (forward.Value, MoveType.Move);

            // Two forward on 1st move
            Index? twoForward = forward.Value.GetNeighborAt(isWhite ? HexNeighborDirection.Up : HexNeighborDirection.Down);
            if (twoForward.HasValue && PawnIsAtStart(team, location) && !boardState.IsOccupied(twoForward.Value))
                yield return (twoForward.Value, MoveType.Move);
        }
    }

    private static bool PawnCanTake(Team team, Index target, BoardState boardState, bool includeBlocking = false)
    {
        if(target == null)
            return false;

        if(boardState.TryGetPiece(target, out (Team team, Piece piece) occupier))
            return occupier.team != team || includeBlocking;

        return false;
    }

    private static bool PawnCanPassant(Team team, Index victimIndex, BoardState boardState)
    {
        if(boardState.TryGetPiece(victimIndex, out (Team team, Piece piece) occupier))
        {
            return occupier.team != team && occupier.piece.IsPawn();
        }
        return false;
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

    private static bool RayCanMove(Team team, BoardState boardState, int row, int col, List<(Index, MoveType)> possible, bool includeBlocking = false)
    {
        Index index = new Index(row, col);
        if (!index.IsInBounds)
            return false;

        if (boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
        {
            if (occupier.team != team || includeBlocking)
                possible.Add((index, MoveType.Attack));
            return false;
        }
        possible.Add((index, MoveType.Move));
        return true;
    }

    private static bool Contains(Piece[] haystack, Piece needle)
    {
        return System.Array.IndexOf(haystack, needle) >= 0;
    }
}
