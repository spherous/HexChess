using System.Collections.Generic;
using System.Linq;
using Extensions;

public static class MoveValidator 
{
    /// <summary>
    /// Is a piece from <paramref name="checkForTeam"/> attacking the enemy king?
    /// </summary>
    /// <param name="checkForTeam"></param>
    /// <returns>true if the enemy king is threatened</returns>
    public static bool IsChecking(Team checkForTeam, BoardState state, List<Promotion> promotions)
    {
        Team enemy = checkForTeam.Enemy();

        if(!state.allPiecePositions.TryGetValue((enemy, Piece.King), out Index enemyKingLoc))
            return false;

        foreach(var rayDirection in EnumArray<HexNeighborDirection>.Values)
        {
            Index? hex = enemyKingLoc;
            (Team team, Piece piece) occupier;
            bool isBishopDirection = rayDirection switch
            {
                HexNeighborDirection.Up => false,
                HexNeighborDirection.Down => false,
                _ => true
            };

            bool isRookDirection = !isBishopDirection;

            bool isPawnDirection = rayDirection switch
            {
                HexNeighborDirection.DownLeft => checkForTeam == Team.White,
                HexNeighborDirection.DownRight => checkForTeam == Team.White,
                HexNeighborDirection.UpLeft => checkForTeam != Team.White,
                HexNeighborDirection.UpRight => checkForTeam != Team.White,
                _ => false
            };

            for(int distance = 1; distance < 20; distance++)
            {
                hex = hex.Value.GetNeighborAt(rayDirection);
                if(!hex.HasValue)
                    break;

                if(state.allPiecePositions.TryGetValue(hex.Value, out occupier))
                {
                    if(occupier.team == checkForTeam)
                    {

                        Piece realPiece = HexachessagonEngine.GetRealPiece(occupier, promotions);

                        if(distance == 1)
                        {
                            if(isPawnDirection && realPiece.IsPawn())
                                return true;

                            if(realPiece == Piece.King)
                                return true;
                        }

                        if(isBishopDirection && (realPiece.IsBishop() || realPiece == Piece.Queen))
                            return true;

                        if(isRookDirection && (realPiece.IsRook() || realPiece == Piece.Queen))
                            return true;

                    }
                    break;
                }
            }
        }

        foreach((Index target, MoveType moveType) move in MoveGenerator.GetAllPossibleMoves(enemyKingLoc, Piece.BlackSquire, enemy, state, promotions))
        {
            if(move.moveType == MoveType.Attack && state.TryGetPiece(move.target, out var occupier))
            {
                Piece realPiece = HexachessagonEngine.GetRealPiece(occupier, promotions);
                if(realPiece.IsSquire())
                    return true;
            }
        }

        foreach((Index target, MoveType moveType) move in MoveGenerator.GetAllPossibleMoves(enemyKingLoc, Piece.KingsKnight, enemy, state, promotions))
        {
            if(move.moveType == MoveType.Attack && state.TryGetPiece(move.target, out var occupier))
            {
                Piece realPiece = HexachessagonEngine.GetRealPiece(occupier, promotions);
                if(realPiece.IsKnight())
                    return true;
            }
        }

        for(int i = 1; i < 20; ++i) // Queen/Rook slide left
        {
            Index hex = new Index(enemyKingLoc.row, enemyKingLoc.col - i);
            if(!hex.IsInBounds)
                break;

            if(state.TryGetPiece(hex, out (Team team, Piece piece) occupier))
            {
                if(occupier.team == checkForTeam)
                {
                    Piece realPiece = HexachessagonEngine.GetRealPiece(occupier, promotions);
                    if(realPiece.IsRook() || realPiece == Piece.Queen)
                        return true;
                }
                break;
            }
        }

        for(int i = 1; i < 20; i++) // Queen/Rook slide right
        {
            Index hex = new Index(enemyKingLoc.row, enemyKingLoc.col + i);
            if(!hex.IsInBounds)
                break;

            if(state.TryGetPiece(hex, out (Team team, Piece piece) occupier))
            {
                if(occupier.team == checkForTeam)
                {
                    Piece realPiece = HexachessagonEngine.GetRealPiece(occupier, promotions);
                    if(realPiece.IsRook() || realPiece == Piece.Queen)
                        return true;
                }
                break;
            }
        }

        return false;
    }
    
    public static BoardState CheckForCheckAndMate(BoardState state, List<Promotion> promotions)
    {
        Team otherTeam = state.currentMove.Enemy();
        if(IsChecking(otherTeam, state, promotions))
        {
            var validMoves = MoveGenerator.GetAllValidMovesForTeam(state.currentMove, state, promotions);
            if(validMoves.Count == 0)
                state.checkmate = state.currentMove;
            else
                state.check = state.currentMove;
        }

        return state;
    }
    
    public static IEnumerable<(Index target, MoveType moveType)> ValidateMoves(IEnumerable<(Index target, MoveType moveType)> possibleMoves, (Team team, Piece piece) teamedPiece, BoardState state, List<Promotion> promotions)
    {
        foreach(var possibleMove in possibleMoves)
        {
            if(IsMoveValid(possibleMove, teamedPiece, state, promotions))
                yield return (possibleMove.target, possibleMove.moveType);
        }
    }
    
    public static bool IsMoveValid((Index target, MoveType moveType) potentialMove, (Team team, Piece piece) teamedPiece, BoardState state, List<Promotion> promotions)
    {
        // A move is invalid if making leaves your king in check
        if(state.TryGetIndex(teamedPiece, out Index startLoc))
        {
            // When checking if a move is valid, we can always assume a queen promotion if applicable 
            // Because no matter what piece a pawn could promote to, it will have the same effect on if the enemy team is still checking you or not
            var newStateWithPromos = HexachessagonEngine.QueryMove(teamedPiece, (potentialMove.target, potentialMove.moveType), state, Piece.Queen, promotions);
            return !IsChecking(teamedPiece.team.Enemy(), newStateWithPromos.newState, newStateWithPromos.promotions);
        }
        return false;
    }

    public static bool HasAnyValidMoves(Team checkForTeam, List<Promotion> promotions, BoardState state, BoardState previousState) => 
        MoveGenerator.GenerateAllValidMoves(checkForTeam, promotions, state, previousState).Any();
}