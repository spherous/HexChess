using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;

public static class MoveGenerator
{
    public static bool IsPromotionRank(Team team, Index target)
    {
        int goal = team == Team.White ? 18 - (target.row % 2) : target.row % 2;
        return target.row == goal;
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

    public static IEnumerable<(Index target, MoveType moveType)> GetAllTheoreticalAttacks(Index location, Piece piece, Team team, BoardState boardState, IEnumerable<Promotion> promos, bool includeBlocking = false) =>
        SkipInvalidMoves(GetTheroreticalAttacksIncludingInvalid(location, piece, team, boardState, promos, includeBlocking));

    public static IEnumerable<(Index target, MoveType moveType)> GetTheroreticalAttacksIncludingInvalid(Index location, Piece piece, Team team, BoardState boardState, IEnumerable<Promotion> promos, bool includeBlocking = false) => piece switch {
        Piece.King => GetAllKingMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece.Queen => GetAllQueenMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece p when p.IsRook() => GetAllRookMovesIncludingInvalid(location, team, boardState, promos, includeBlocking),
        Piece p when p.IsKnight() => GetAllKnightMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece p when p.IsBishop() => GetAllBishopMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece p when p.IsSquire() => GetAllSquireMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece p when p.IsPawn() => GetAllPawnTheoreticalAttacksIncludingInvalid(location, team, boardState, includeBlocking),
        _ => throw new ArgumentException($"Unhandled piece type: {piece}", nameof(piece))
    };

    public static IEnumerable<(Index target, MoveType moveType)> GetAllPossibleMoves(Index location, Piece piece, Team team, BoardState boardState, IEnumerable<Promotion> promos, bool includeBlocking = false) => 
        SkipInvalidMoves(GetAllMovesIncludingInvalid(location, piece, team, boardState, promos, includeBlocking));

    public static IEnumerable<(Index target, MoveType moveType)> GetAllMovesIncludingInvalid(Index location, Piece piece, Team team, BoardState boardState, IEnumerable<Promotion> promos, bool includeBlocking = false) => piece switch {
        Piece.King => GetAllKingMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece.Queen => GetAllQueenMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece p when p.IsRook() => GetAllRookMovesIncludingInvalid(location, team, boardState, promos, includeBlocking),
        Piece p when p.IsKnight() => GetAllKnightMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece p when p.IsBishop() => GetAllBishopMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece p when p.IsSquire() => GetAllSquireMovesIncludingInvalid(location, team, boardState, includeBlocking),
        Piece p when p.IsPawn() => GetAllPawnMovesIncludingInvalid(location, team, boardState, includeBlocking),
        _ => throw new ArgumentException($"Unhandled piece type: {piece}", nameof(piece))
    };

    public static IEnumerable<(Index, MoveType)> SkipInvalidMoves(IEnumerable<(Index index, MoveType type)> allMovesIncludingInvalid) => 
        allMovesIncludingInvalid.Where(move => move.type != MoveType.None);

    public static IEnumerable<(Index, MoveType)> GetAllKingMovesIncludingInvalid(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        foreach(HexNeighborDirection dir in EnumArray<HexNeighborDirection>.Values)
        {
            Index? maybeIndex = location.GetNeighborAt(dir);
            if(maybeIndex == null)
            {
                yield return (Index.invalid, MoveType.None);
                continue;
            }

            Index index = maybeIndex.Value;

            if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
            {
                if(includeBlocking || occupier.team != team)
                {
                    yield return (index, MoveType.Attack);
                    continue;
                }
                else
                {
                    yield return (Index.invalid, MoveType.None);
                    continue;
                }
            }
            yield return (index, MoveType.Move);
        }
    }

    public static IEnumerable<(Index, MoveType)> GetAllSquireMovesIncludingInvalid(Index location, Team team, BoardState boardState, bool includeBlocking = false)
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
            {
                yield return (Index.invalid, MoveType.None);
                continue;
            }

            if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
            {
                if(occupier.team == team && !includeBlocking)
                {
                    yield return (Index.invalid, MoveType.None);
                    continue;
                }

                yield return (index, MoveType.Attack);
            }
            else
                yield return (index, MoveType.Move);
        }
    }

    public static IEnumerable<(Index, MoveType)> GetAllKnightMovesIncludingInvalid(Index location, Team team, BoardState boardState, bool includeBlocking = false)
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
            if(!index.IsInBounds)
            {
                yield return (Index.invalid, MoveType.None);
                continue;
            }

            if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
            {
                if(includeBlocking || occupier.team != team)
                    yield return (index, MoveType.Attack);
                else
                    yield return (Index.invalid, MoveType.None);
            }
            else
                yield return(index, MoveType.Move);
        }
    }

    public static IEnumerable<(Index, MoveType)> GetAllBishopMovesIncludingInvalid(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        List<(Index, MoveType)> possible = new List<(Index, MoveType)>();
        int offset = location.row % 2;

        // Top Left
        for(
            (int row, int col, int i, bool blocked) = (location.row + 1, location.col - offset, 0, false);
            i < 8;
            row++, i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, row, col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));

            if(i % 2 == offset)
                col--;
        }

        // Top Right
        for(
            (int row, int col, int i, bool blocked) = (location.row + 1, location.col + Math.Abs(1 - offset), 0, false);
            i < 8;
            row++, i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, row, col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));

            if(i % 2 != offset)
                col++;
        }
        // Bottom Left
        for(
            (int row, int col, int i, bool blocked) = (location.row - 1, location.col - offset, 0, false);
            i < 8;
            row--, i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, row, col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));

            if(i % 2 == offset)
                col--;
        }
        // Bottom Right
        for(
            (int row, int col, int i, bool blocked) = (location.row - 1, location.col + Math.Abs(1 - offset), 0, false);
            i < 8;
            row--, i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, row, col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));
                
            if(i % 2 != offset)
                col++;
        }

        return possible;
    }

    public static IEnumerable<(Index, MoveType)> GetAllRookMovesIncludingInvalid(Index location, Team team, BoardState boardState, IEnumerable<Promotion> promos, bool includeBlocking = false)
    {
        List<(Index, MoveType)> possible = new List<(Index, MoveType)>();
        
        // Up
        for(
            (int i, bool blocked) = (1, false);
            i <= 9;
            i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, location.row + 2 * i, location.col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));
        }

        // Down
        for(
            (int i, bool blocked) = (1, false);
            i <= 9;
            i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, location.row - 2 * i, location.col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));
        }

        // Left
        for(
            (int i, bool blocked) = (1, false);
            i <= 4;
            i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, location.row, location.col - i, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));
        }

        // Right
        for(
            (int i, bool blocked) = (1, false);
            i <= 4;
            i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, location.row, location.col + i, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));
        }

        // Check defend
        foreach(HexNeighborDirection dir in EnumArray<HexNeighborDirection>.Values)
        {
            if(dir == HexNeighborDirection.Up || dir == HexNeighborDirection.Down)
            {
                // Up is always possible[0]
                // down is always possible[9]
                int i = dir == HexNeighborDirection.Up ? 0 : 9;

                Index? maybeIndex = location.GetNeighborAt(dir);
                if(maybeIndex == null)
                {
                    possible[i] = (Index.invalid, MoveType.None);
                    continue;
                }

                Index index = maybeIndex.Value;

                if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
                {
                    // can defend -- this should account for promoted pawns as well, todo
                    IEnumerable<Promotion> applicablePromos = promos.Where(promo => promo.team == occupier.team && promo.from == occupier.piece);
                    Piece realPiece = applicablePromos.Any() ? applicablePromos.First().from : occupier.piece;
                    
                    if(occupier.team == team)
                    {
                        if(Contains(DefendableTypes, realPiece))
                            possible[i] = (index, MoveType.Defend);
                        // allied, non-defendable piece
                        else if(includeBlocking)
                            possible[i] = (index, MoveType.Attack);
                        else
                            possible[i] = (Index.invalid, MoveType.None);
                    }
                    // else it's an attack, keep as is
                }
            }
            else
            {
                Index? maybeIndex = location.GetNeighborAt(dir);
                if(maybeIndex == null)
                {
                    possible.Add((Index.invalid, MoveType.None));
                    continue;
                }
                Index index = maybeIndex.Value;

                if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
                {
                    if(occupier.team == team && Contains(DefendableTypes, occupier.piece))
                        possible.Add((index, MoveType.Defend));
                    else
                        possible.Add((Index.invalid, MoveType.None));
                }
                else
                    possible.Add((Index.invalid, MoveType.None));
            }
        }

        return possible;
    }

    public static IEnumerable<(Index, MoveType)> GetAllQueenMovesIncludingInvalid(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        List<(Index, MoveType)> possible = new List<(Index, MoveType)>();
        int offset = location.row % 2;

        // Up
        for(
            (int i, bool blocked) = (1, false);
            i <= 9;
            i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, location.row + 2 * i, location.col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));
        }

        // Down
        for(
            (int i, bool blocked) = (1, false);
            i <= 9;
            i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, location.row - 2 * i, location.col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));
        }

        // Left
        for(
            (int i, bool blocked) = (1, false);
            i <= 4;
            i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, location.row, location.col - i, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));
        }

        // Right
        for(
            (int i, bool blocked) = (1, false);
            i <= 4;
            i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, location.row, location.col + i, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));
        }

        // Top Left
        for(
            (int row, int col, int i, bool blocked) = (location.row + 1, location.col - offset, 0, false);
            i < 8;
            row++, i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, row, col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));

            if(i % 2 == offset)
                col--;
        }

        // Top Right
        for(
            (int row, int col, int i, bool blocked) = (location.row + 1, location.col + Math.Abs(1 - offset), 0, false);
            i < 8;
            row++, i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, row, col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));

            if(i % 2 != offset)
                col++;
        }
        // Bottom Left
        for(
            (int row, int col, int i, bool blocked) = (location.row - 1, location.col - offset, 0, false);
            i < 8;
            row--, i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, row, col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));

            if(i % 2 == offset)
                col--;
        }
        // Bottom Right
        for(
            (int row, int col, int i, bool blocked) = (location.row - 1, location.col + Math.Abs(1 - offset), 0, false);
            i < 8;
            row--, i++
        ){
            if(!blocked)
                blocked = !RayCanMove(team, boardState, row, col, possible, includeBlocking);
            else
                possible.Add((Index.invalid, MoveType.None));

            if(i % 2 != offset)
                col++;
        }

        return possible;
    }

    #region Pawn
    public static IEnumerable<(Index, MoveType)> GetAllPawnMovesIncludingInvalid(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        bool isWhite = team == Team.White;
        Index? leftAttack = location.GetNeighborAt(isWhite ? HexNeighborDirection.UpLeft : HexNeighborDirection.DownLeft);
        Index? rightAttack = location.GetNeighborAt(isWhite ? HexNeighborDirection.UpRight : HexNeighborDirection.DownRight);

        // Check takes
        bool canTakeLeft = leftAttack.HasValue && PawnCanTake(team, leftAttack.Value, boardState, includeBlocking);
        bool canTakeRight = rightAttack.HasValue && PawnCanTake(team, rightAttack.Value, boardState, includeBlocking);

        // Check en passant
        Index? leftPassant = location.GetNeighborAt(isWhite ? HexNeighborDirection.DownLeft : HexNeighborDirection.UpLeft);
        Index? rightPassant = location.GetNeighborAt(isWhite ? HexNeighborDirection.DownRight : HexNeighborDirection.UpRight);

        bool canPassantLeft = leftAttack.HasValue && leftPassant.HasValue && PawnCanPassant(team, leftPassant.Value, boardState);
        bool canPassantRight = rightAttack.HasValue && rightPassant.HasValue && PawnCanPassant(team, rightPassant.Value, boardState);

        if(canTakeLeft)
            yield return (leftAttack.Value, MoveType.Attack);
        else if(canPassantLeft)
            yield return (leftAttack.Value, MoveType.EnPassant);
        else
            yield return (Index.invalid, MoveType.None);
        
        if(canTakeRight)
            yield return (rightAttack.Value, MoveType.Attack);
        else if(canPassantRight)
            yield return (rightAttack.Value, MoveType.EnPassant);
        else
            yield return (Index.invalid, MoveType.None);

        // One forward
        Index? forward = location.GetNeighborAt(isWhite ? HexNeighborDirection.Up : HexNeighborDirection.Down);
        if(forward.HasValue && !boardState.IsOccupied(forward.Value))
        {
            yield return (forward.Value, MoveType.Move);

            // Two forward on 1st move
            Index? twoForward = forward.Value.GetNeighborAt(isWhite ? HexNeighborDirection.Up : HexNeighborDirection.Down);
            if(twoForward.HasValue && PawnIsAtStart(team, location) && !boardState.IsOccupied(twoForward.Value))
                yield return (twoForward.Value, MoveType.Move);
            else
                yield return (Index.invalid, MoveType.None);
        }
        else
        {
            yield return (Index.invalid, MoveType.None);
            yield return (Index.invalid, MoveType.None);
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
            Index occupierStartLoc = HexachessagonEngine.GetStartLocation(occupier);
            
            // A pawn is only passantable if it's current location is 2 hexes straight from it's start location
            HexNeighborDirection dir = occupier.team == Team.White ? HexNeighborDirection.Up : HexNeighborDirection.Down;
            Index? passantableLocation = occupierStartLoc.GetNeighborAt(dir)?.GetNeighborAt(dir);

            bool pawnInProperLoc = passantableLocation.HasValue && victimIndex == passantableLocation.Value;

            return occupier.team != team && occupier.piece.IsPawn() && pawnInProperLoc;
        }
        else
            return false;
    }

    private static bool PawnIsAtStart(Team team, Index location) => team == Team.White 
        ? location.row == 3 || location.row == 4 
        : location.row == 14 || location.row == 15;

    public static IEnumerable<(Index, MoveType)> GetAllPawnTheoreticalAttacksIncludingInvalid(Index location, Team team, BoardState boardState, bool includeBlocking = false)
    {
        bool isWhite = team == Team.White;
        Index? leftAttack = location.GetNeighborAt(isWhite ? HexNeighborDirection.UpLeft : HexNeighborDirection.DownLeft);
        Index? rightAttack = location.GetNeighborAt(isWhite ? HexNeighborDirection.UpRight : HexNeighborDirection.DownRight);

        // Check takes
        bool canTakeLeft = leftAttack.HasValue;
        bool canTakeRight = rightAttack.HasValue;

        // Check en passant
        Index? leftPassant = location.GetNeighborAt(isWhite ? HexNeighborDirection.DownLeft : HexNeighborDirection.UpLeft);
        Index? rightPassant = location.GetNeighborAt(isWhite ? HexNeighborDirection.DownRight : HexNeighborDirection.UpRight);
        bool canPassantLeft = leftAttack.HasValue && leftPassant.HasValue;
        bool canPassantRight = rightAttack.HasValue && rightPassant.HasValue;

        if(canTakeLeft)
            yield return (leftAttack.Value, MoveType.Attack);
        else if(canPassantLeft)
            yield return (leftAttack.Value, MoveType.EnPassant);
        else
            yield return (Index.invalid, MoveType.None);
        
        if(canTakeRight)
            yield return (rightAttack.Value, MoveType.Attack);
        else if(canPassantRight)
            yield return (rightAttack.Value, MoveType.EnPassant);
        else
            yield return (Index.invalid, MoveType.None);

        // One forward
        // Index? forward = location.GetNeighborAt(isWhite ? HexNeighborDirection.Up : HexNeighborDirection.Down);
        // if(forward.HasValue && !boardState.IsOccupied(forward.Value))
        // {
        //     yield return (forward.Value, MoveType.Move);

        //     // Two forward on 1st move
        //     Index? twoForward = forward.Value.GetNeighborAt(isWhite ? HexNeighborDirection.Up : HexNeighborDirection.Down);
        //     if(twoForward.HasValue && PawnIsAtStart(team, location) && !boardState.IsOccupied(twoForward.Value))
        //         yield return (twoForward.Value, MoveType.Move);
        //     else
        //         yield return (Index.invalid, MoveType.None);
        // }
        // else
        // {
        //     yield return (Index.invalid, MoveType.None);
        //     yield return (Index.invalid, MoveType.None);
        // }
    }
    #endregion

    private static bool RayCanMove(Team team, BoardState boardState, int row, int col, List<(Index, MoveType)> possible, bool includeBlocking = false)
    {
        Index index = new Index(row, col);
        if(!index.IsInBounds)
        {
            possible.Add((Index.invalid, MoveType.None));
            return false;
        }

        if(boardState.TryGetPiece(index, out (Team team, Piece piece) occupier))
        {
            if(occupier.team != team || includeBlocking)
                possible.Add((index, MoveType.Attack));
            else
                possible.Add((Index.invalid, MoveType.None));
            return false;
        }
        possible.Add((index, MoveType.Move));
        return true;
    }

    private static bool Contains(Piece[] haystack, Piece needle) => System.Array.IndexOf(haystack, needle) >= 0;

    public static IEnumerable<(Index start, Index target, MoveType moveType, Piece promoteTo)> GenerateAllValidMoves(Team checkForTeam, List<Promotion> promotions, BoardState state, BoardState previousState)
    {
        Team enemyTeam = checkForTeam.Enemy();
        foreach(KeyValuePair<(Team team, Piece piece), Index> kvp in state.allPiecePositions)
        {
            if(kvp.Key.team != checkForTeam)
                continue;

            Piece realPiece = HexachessagonEngine.GetRealPiece(kvp.Key, promotions);
            var pieceMoves = MoveGenerator.GetAllPossibleMoves(kvp.Value, realPiece, kvp.Key.team, state, promotions);
            foreach((Index target, MoveType moveType) potentialMove in pieceMoves)
            {
                if(potentialMove.moveType == MoveType.EnPassant)
                {
                    if(!potentialMove.target.TryGetNeighbor(checkForTeam == Team.White ? HexNeighborDirection.Up : HexNeighborDirection.Down, out Index enemyStartLoc))
                        continue;

                    if(state.IsOccupied(enemyStartLoc))
                        continue;

                    if(!previousState.TryGetPiece(enemyStartLoc, out var victim))
                        continue;

                    Piece realVictim = HexachessagonEngine.GetRealPiece(victim, promotions);
                    if(!realVictim.IsPawn())
                        continue;
                }

                // What we promote to doesn't matter for the purpose of determining enemy checks
                (BoardState newState, List<Promotion> newPromotions) = HexachessagonEngine.QueryMove(kvp.Key, (potentialMove.target, potentialMove.moveType), state, Piece.Pawn1, promotions);
                if(MoveValidator.IsChecking(enemyTeam, newState, newPromotions))
                    continue;

                if(realPiece.IsPawn() && MoveGenerator.IsPromotionRank(checkForTeam, potentialMove.target))
                {
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.Queen);
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.QueensBishop);
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.QueensRook);
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.QueensKnight);
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.BlackSquire);
                }
                else
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.Pawn1);
            }
        }
    }

    public static List<(Piece piece, Index index, MoveType moveType)> GetAllValidMovesForTeam(Team team, BoardState state, List<Promotion> promotions)
    {
        List<(Piece, Index, MoveType)> validMoves = new List<(Piece, Index, MoveType)>();
        IEnumerable<Piece> remainingPiecesForTeam = HexachessagonEngine.GetRemainingPiecesForTeam(team, state, promotions);
        foreach(Piece piece in remainingPiecesForTeam)
        {
            var moves = GetAllValidMovesForPiece((team, piece), state, promotions);
            validMoves.AddRange(moves.Select(move => (piece, move.target, move.moveType)));
        }
        return validMoves;
    }

    public static IEnumerable<(Index target, MoveType moveType)> GetAllValidMovesForPiece((Team team, Piece piece) teamedPiece, BoardState boardState, List<Promotion> promotions, bool includeBlocking = false)
    {
        if(boardState.TryGetIndex(teamedPiece, out Index location))
        {
            Piece realPiece = HexachessagonEngine.GetRealPiece(location, boardState, promotions);

            IEnumerable<(Index, MoveType)> possibleMoves = MoveGenerator.GetAllPossibleMoves(location, realPiece, teamedPiece.team, boardState, promotions, includeBlocking);
            return MoveValidator.ValidateMoves(possibleMoves, teamedPiece, boardState, promotions);
        }
        return Enumerable.Empty<(Index target, MoveType moveType)>();
    }

    public static IEnumerable<Index> GetValidAttacksConcerningHex(Index hexIndex, BoardState state, List<Promotion> promotions) => state.allPiecePositions
        .Where(kvp => GetAllValidAttacksForPieceConcerningHex((kvp.Key), state, hexIndex, promotions, true)
            .Any(targetIndex => targetIndex == hexIndex)
        ).Select(kvp => kvp.Value);
    
    public static IEnumerable<Index> GetAllValidAttacksForPieceConcerningHex((Team team, Piece piece) teamedPiece, BoardState boardState, Index hexIndex, List<Promotion> promotions, bool includeBlocking = false)
    {
        if(boardState.TryGetIndex(teamedPiece, out Index location))
        {
            IEnumerable<(Index target, MoveType moveType)> possibleMoves = MoveGenerator.GetAllPossibleMoves(location, HexachessagonEngine.GetRealPiece(teamedPiece, promotions), teamedPiece.team, boardState, promotions, includeBlocking)
                .Where(kvp => kvp.target != null && kvp.target == hexIndex)
                .Where(kvp => kvp.moveType == MoveType.Attack || kvp.moveType == MoveType.EnPassant);

            return MoveValidator.ValidateMoves(possibleMoves, teamedPiece, boardState, promotions).Select(validMove => validMove.target);
        }
        return Enumerable.Empty<Index>();
    }

    public static IEnumerable<Index> GetAllValidTheoreticalAttacksFromTeamConcerningHex(Team team, Index hexIndex, BoardState state, List<Promotion> promotions) => state.allPiecePositions
        .Where(kvp => GetAllTheoreticalAttacksForPieceConcerningHex(kvp.Key, state, hexIndex, promotions, true)
            .Any(targetIndex => targetIndex == hexIndex) && kvp.Key.Item1 == team
        ).Select(kvp => kvp.Value);
    
    public static IEnumerable<Index> GetAllTheoreticalAttacksForPieceConcerningHex((Team team, Piece piece) teamedPiece, BoardState boardState, Index hexIndex, List<Promotion> promotions, bool includeBlocking = false)
    {
        if(boardState.TryGetIndex(teamedPiece, out Index location))
        {
            IEnumerable<(Index target, MoveType moveType)> possibleMoves = MoveGenerator.GetAllTheoreticalAttacks(location, teamedPiece.piece, teamedPiece.team, boardState, promotions, includeBlocking)
                .Where(kvp => kvp.target != null && kvp.target == hexIndex)
                .Where(kvp => kvp.moveType != MoveType.Defend);
            return MoveValidator.ValidateMoves(possibleMoves, teamedPiece, boardState, promotions).Select(validMove => validMove.target);
        }
        return Enumerable.Empty<Index>();
    }
}
