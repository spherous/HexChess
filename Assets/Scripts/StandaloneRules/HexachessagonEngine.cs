using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;

public static class HexachessagonEngine
{
    public static readonly List<List<Piece>> insufficientSets = new List<List<Piece>>(){
        new List<Piece>(){Piece.King},
        new List<Piece>(){Piece.King, Piece.WhiteSquire},
        new List<Piece>(){Piece.King, Piece.GraySquire},
        new List<Piece>(){Piece.King, Piece.BlackSquire},
        new List<Piece>(){Piece.King, Piece.WhiteSquire, Piece.BlackSquire},
    };
    
    public static Index GetStartLocation((Team team, Piece piece) teamedPiece)
    {
        BoardState defaultState = BoardState.defaultState;
        if(defaultState.TryGetIndex(teamedPiece, out Index index))
            return index;
        else
            return Index.invalid;
    }

    public static Piece GetRealPiece(Index index, BoardState state, List<Promotion> promotions) => GetRealPiece(state.allPiecePositions[index], promotions);
    public static Piece GetRealPiece((Team team, Piece piece) teamedPiece, List<Promotion> promotions, int? turnNumber = null)
    {
        if(promotions != null && teamedPiece.piece.IsPawn())
        {
            foreach(var promotion in promotions)
            {
                // If the turn number isn't passed in, we just want the promoted piece since we have nothing to compare to
                if(promotion.from == teamedPiece.piece && promotion.team == teamedPiece.team)
                    return turnNumber.HasValue && promotion.turnNumber > turnNumber.Value ? teamedPiece.piece : promotion.to;
            }
        }

        return teamedPiece.piece;
    }

    public static bool TryGetApplicablePromo((Team team, Piece piece) teamedPiece, int turnNumber, out Promotion promotion, List<Promotion> promotions)
    {
        IEnumerable<Promotion> potential = promotions.Where(promo => promo.turnNumber <= turnNumber && promo.team == teamedPiece.team && promo.from == teamedPiece.piece);
        promotion = potential.FirstOrDefault();
        return potential.Any();
    }

    public static BoardState Enprison(BoardState currentState, (Team team, Piece piece) teamedPiece)
    {
        var allPiecePositions = currentState.allPiecePositions.Clone();
        allPiecePositions.Remove(teamedPiece);
        currentState.allPiecePositions = allPiecePositions;
        currentState.currentMove = currentState.currentMove.Enemy();
        return currentState;
    }

    public static Move GetLastMove(List<BoardState> history, List<Promotion> promotions, bool isFreeplaced = false)
    {
        if(history.Count > 1)
        {
            BoardState lastState = history[history.Count - 2];
            BoardState nowState = history[history.Count - 1];
            
            foreach(KeyValuePair<(Team team, Piece piece), Index> kvp in lastState.allPiecePositions.Where(k => k.Key.team == lastState.currentMove))
            {
                Piece piece = kvp.Key.piece;

                if(!nowState.TryGetIndex(kvp.Key, out Index nowPos))
                {
                    // This case happens when using free place mode and removing a piece from the board into the jail
                    if(isFreeplaced)
                        return new Move(
                            turn: history.Count / 2,
                            lastTeam: kvp.Key.team,
                            lastPiece: piece,
                            from: kvp.Value,
                            to: Index.invalid,
                            capturedPiece: piece,
                            defendedPiece: null,
                            duration: nowState.executedAtTime - lastState.executedAtTime
                        );
                    else
                        continue;
                }

                if(kvp.Value == nowPos)
                    continue;

                (Team previousTeamAtLocation, Piece? previousPieceAtLocation) = lastState.allPiecePositions.Contains(nowPos)
                    ? lastState.allPiecePositions[nowPos]
                    : (Team.None, (Piece?)null);

                Piece? capturedPiece = previousTeamAtLocation == kvp.Key.team ? null : previousPieceAtLocation;
                if(piece.IsPawn() && kvp.Value.GetLetter() != nowPos.GetLetter() && capturedPiece == null)
                {
                    // Pawns that move sideways are always attacks. If the new location was unoccupied, then did En Passant
                    Index? enemyLocation = nowPos.GetNeighborAt(kvp.Key.team == Team.White ? HexNeighborDirection.Down : HexNeighborDirection.Up);
                    if(enemyLocation != null && lastState.TryGetPiece(enemyLocation.Value, out var captured))
                        capturedPiece = captured.piece;
                }

                Index from = kvp.Value;
                Index to = nowPos;

                // In the case of a defend, we may check the piece being defended before the rook doing the defending. 
                // If this is the case, we need to ensure the piece is the rook and the defended piece is the non-rook, as well as the proper to/from indcies
                Piece? defendedPiece = previousTeamAtLocation != kvp.Key.team ? null : (Piece?)HexachessagonEngine.GetRealPiece(kvp.Key, promotions);
                if(defendedPiece != null)
                {
                    if(defendedPiece == Piece.QueensRook || defendedPiece == Piece.KingsRook)
                        (defendedPiece, piece) = (previousPieceAtLocation, defendedPiece.Value);
                    else
                    {
                        piece = previousPieceAtLocation.Value;
                        (to, from) = (from, to);
                    }
                }

                return new Move(
                    turn: history.Count / 2,
                    lastTeam: kvp.Key.team,
                    lastPiece: piece,
                    from: from,
                    to: to,
                    capturedPiece: capturedPiece,
                    defendedPiece: defendedPiece,
                    duration: nowState.executedAtTime - lastState.executedAtTime
                );
            }

            // check if any piece didn't have a last pos but does now, this is a piece freed from jail
            foreach(var kvp in nowState.allPiecePositions)
            {
                if(!lastState.TryGetIndex(kvp.Key, out Index lastIndex))
                {
                    if(isFreeplaced)
                        return new Move(
                            turn: history.Count / 2,
                            lastTeam: kvp.Key.team,
                            lastPiece: kvp.Key.piece,
                            from: Index.invalid,
                            to: kvp.Value,
                            capturedPiece: null,
                            defendedPiece: null,
                            duration: nowState.executedAtTime - lastState.executedAtTime
                        );
                    else
                        continue;
                }
                else
                    continue;
            }

            // No piece moved. Most likely turn was passed with free place mode.
            return new Move(
                turn: history.Count / 2,
                lastTeam: lastState.currentMove,
                lastPiece: Piece.King,
                from: Index.invalid,
                to: Index.invalid,
                capturedPiece: null,
                defendedPiece: null,
                duration: nowState.executedAtTime - lastState.executedAtTime
            );
        }
        return new Move(0, Team.None, Piece.King, Index.invalid, Index.invalid);
    }

    public static (BoardState newState, List<Promotion> promotions) QueryMove(Index start, (Index target, MoveType moveType) move, BoardState state, Piece promoteTo, List<Promotion> promotions, int turnNumber = 1)
    {
        if(state.TryGetPiece(start, out (Team team, Piece piece) teamedPiece))
            return QueryMove(teamedPiece, move, state, promoteTo, promotions, turnNumber);
        else
            throw new Exception($"Invalid start location ({start}) for piece. ");
    }

    public static (BoardState newState, List<Promotion> promotions) QueryMove((Team team, Piece piece) teamedPiece, (Index target, MoveType moveType) move, BoardState state, Piece promoteTo, List<Promotion> promotions, int turnNumber = 1)
    {
        switch(move.moveType)
        {
            case MoveType.Move:
            case MoveType.Attack:
                return QueryMoveOrAttack(teamedPiece, move, state, promotions, promoteTo, turnNumber);
            case MoveType.Defend:
                return QueryDefend(teamedPiece, move, state, promotions);
            case MoveType.EnPassant:
                return QueryEnPassant(teamedPiece, move, state, promotions);
            default:
                throw new Exception($"Invalid move type: {move.moveType}");
        }
    }

    public static (BoardState newState, List<Promotion> promotions) QueryMoveOrAttack((Team team, Piece piece) teamedPiece, (Index target, MoveType moveType) move, BoardState state, List<Promotion> promotions, Piece promoteTo, int turnNumber)
    {        
        var newPositions = state.allPiecePositions.Clone();
        if(state.TryGetIndex(teamedPiece, out Index start))
            newPositions.Remove(start);
        newPositions.Remove(move.target);
        newPositions.Add(teamedPiece, move.target);

        List<Promotion> newPromotions;
        if(!promoteTo.IsPawn() && MoveGenerator.IsPromotionRank(teamedPiece.team, move.target))
        {
            newPromotions = (promotions == null) ? new List<Promotion>(1) : new List<Promotion>(promotions);
            // Since we're simulating a move early (a query), if it was white's turn, we need to increment the turn number to have a correct promotion turn
            turnNumber = state.currentMove == Team.White ? turnNumber + 1 : turnNumber;
            newPromotions.Add(new Promotion(teamedPiece.team, teamedPiece.piece, promoteTo, turnNumber));
        }
        else
            newPromotions = promotions;

        var newState = new BoardState(newPositions, state.currentMove.Enemy(), state.check, state.checkmate, state.executedAtTime);
        return (newState, newPromotions);
    }
    public static (BoardState newState, List<Promotion> promotions) QueryDefend((Team team, Piece piece) teamedPiece, (Index target, MoveType moveType) move, BoardState state, List<Promotion> promotions)
    {
        // A defend can't come from the jail via freplace mode. It is only allowed with pieces on the board, thus a start pos is absolutely required
        if(!state.TryGetIndex(teamedPiece, out Index start))
            return (state, promotions);

        var targetPiece = state.allPiecePositions[move.target];
        var newPositions = state.allPiecePositions.Clone();
        newPositions.Remove(start);
        newPositions.Remove(move.target);
        newPositions.Add(teamedPiece, move.target);
        newPositions.Add(targetPiece, start);

        var newState = new BoardState(newPositions, state.currentMove.Enemy(), state.check, state.checkmate, state.executedAtTime);
        return (newState, promotions);
    }
    public static (BoardState newState, List<Promotion> promotions) QueryEnPassant((Team team, Piece piece) teamedPiece, (Index target, MoveType moveType) move, BoardState state, List<Promotion> promotions)
    {
        var victimLocation = move.target.GetNeighborAt(teamedPiece.team == Team.White ? HexNeighborDirection.Down : HexNeighborDirection.Up).Value;
        var newPositions = state.allPiecePositions.Clone();

        if(state.TryGetIndex(teamedPiece, out Index start))
            newPositions.Remove(start);
        newPositions.Remove(victimLocation);
        newPositions.Add(teamedPiece, move.target);

        var newState = new BoardState(newPositions, state.currentMove.Enemy(), state.check, state.checkmate, state.executedAtTime);
        return (newState, promotions);
    }

    public static IEnumerable<Piece> GetRemainingPiecesForTeam(Team team, BoardState state, List<Promotion> promotions) =>
        state.allPiecePositions.Where(kvp => kvp.Key.Item1 == team).Select(kvp => {
            IEnumerable<Promotion> applicablePromos = promotions.Where(promo => promo.from == kvp.Key.Item2 && promo.team == team);
            if(applicablePromos.Any())
                return applicablePromos.First().to;
            return kvp.Key.Item2;
        });
}