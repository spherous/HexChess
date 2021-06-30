using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Extensions;

public struct BoardState
{
    public Team currentMove;
    public BidirectionalDictionary<(Team team, Piece piece), Index> allPiecePositions;

    public Team check;
    public Team checkmate;
    public float executedAtTime;

    public BoardState(BidirectionalDictionary<(Team, Piece), Index> allPiecePositions, Team currentMove, Team check, Team checkmate, float executedAtTime)
    {
        this.allPiecePositions = allPiecePositions;
        this.currentMove = currentMove;
        this.check = check;
        this.checkmate = checkmate;
        this.executedAtTime = executedAtTime;
    }

    public static Move GetLastMove(List<BoardState> history)
    {
        if(history.Count > 1)
        {
            BoardState lastState = history[history.Count - 2];
            BoardState nowState = history[history.Count - 1];
            foreach(KeyValuePair<(Team team, Piece piece), Index> kvp in lastState.allPiecePositions)
            {
                if(!nowState.TryGetIndex(kvp.Key, out Index nowPos))
                    continue;

                if(kvp.Value == nowPos)
                    continue;

                (Team previousTeamAtLocation, Piece? previousPieceAtLocation) = lastState.allPiecePositions.Contains(nowPos)
                    ? lastState.allPiecePositions[nowPos]
                    : (Team.None, (Piece?)null);

                Piece? capturedPiece = previousTeamAtLocation == kvp.Key.team ? null : previousPieceAtLocation;
                if(kvp.Key.piece.IsPawn() && kvp.Value.GetLetter() != nowPos.GetLetter() && capturedPiece == null)
                {
                    // Pawns that move sideways are always attacks. If the new location was unoccupied, then did En Passant
                    Index? enemyLocation = nowPos.GetNeighborAt(kvp.Key.team == Team.White ? HexNeighborDirection.Down : HexNeighborDirection.Up);
                    if (enemyLocation != null && lastState.TryGetPiece(enemyLocation.Value, out var captured))
                        capturedPiece = captured.piece;
                }

                return new Move(
                    turn: history.Count / 2,
                    lastTeam: kvp.Key.team,
                    lastPiece: kvp.Key.piece,
                    from: kvp.Value,
                    to: nowPos,
                    capturedPiece: capturedPiece,
                    defendedPiece: previousTeamAtLocation != kvp.Key.team ? null : previousPieceAtLocation,
                    duration: nowState.executedAtTime - lastState.executedAtTime
                );
                
            }
        }
        return new Move(0, Team.None, Piece.King, default(Index), default(Index));
    }

    public readonly bool TryGetPiece(Index index, out (Team team, Piece piece) teamedPiece) => 
        allPiecePositions.TryGetValue(index, out teamedPiece);
    public readonly bool TryGetIndex((Team team, Piece piece) teamedPiece, out Index index) => 
        allPiecePositions.TryGetValue(teamedPiece, out index);

    public static bool operator ==(BoardState a, BoardState b) => 
        a.currentMove == b.currentMove 
        && a.allPiecePositions.Count == b.allPiecePositions.Count 
        && !a.allPiecePositions.Except(b.allPiecePositions).Any();
    public static bool operator !=(BoardState a, BoardState b) => !(a == b);
    
    public override bool Equals(object obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();

    public readonly bool IsOccupied(Index index)
    {
        return allPiecePositions.ContainsKey(index);
    }
    public readonly bool IsOccupiedBy(Index index, (Team expectedTeam, Piece expectedPiece) expected)
    {
        if (!TryGetPiece(index, out (Team actualTeam, Piece actualPiece) actual))
            return false;

        return expected == actual;
    }

    /// <summary>
    /// Is a piece from <paramref name="checkForTeam"/> attacking the enemy king?
    /// </summary>
    /// <param name="checkForTeam"></param>
    /// <returns>true if the enemy king is threatened</returns>
    public bool IsChecking(Team checkForTeam, List<Promotion> promotions)
    {
        Team enemy = checkForTeam.Enemy();

        if (!allPiecePositions.TryGetValue((enemy, Piece.King), out Index enemyKingLoc))
            return false;

        foreach (var rayDirection in EnumArray<HexNeighborDirection>.Values)
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

            for (int distance = 1; distance < 20; distance++)
            {
                hex = hex.Value.GetNeighborAt(rayDirection);
                if (!hex.HasValue)
                    break;

                if (allPiecePositions.TryGetValue(hex.Value, out occupier))
                {
                    if (occupier.team == checkForTeam)
                    {

                        Piece realPiece = GetRealPiece(occupier, promotions);

                        if (distance == 1)
                        {
                            if (isPawnDirection && realPiece.IsPawn())
                                return true;

                            if (realPiece == Piece.King)
                                return true;
                        }

                        if (isBishopDirection && (realPiece.IsBishop() || realPiece == Piece.Queen))
                            return true;

                        if (isRookDirection && (realPiece.IsRook() || realPiece == Piece.Queen))
                            return true;

                    }
                    break;
                }
            }
        }

        foreach ((Index target, MoveType moveType) move in MoveGenerator.GetAllPossibleSquireMoves(enemyKingLoc, enemy, this))
        {
            if (move.moveType == MoveType.Attack && TryGetPiece(move.target, out var occupier))
            {
                Piece realPiece = GetRealPiece(occupier, promotions);
                if (realPiece.IsSquire())
                    return true;
            }
        }

        foreach ((Index target, MoveType moveType) move in MoveGenerator.GetAllPossibleKnightMoves(enemyKingLoc, enemy, this))
        {
            if (move.moveType == MoveType.Attack && TryGetPiece(move.target, out var occupier))
            {
                Piece realPiece = GetRealPiece(occupier, promotions);
                if (realPiece.IsKnight())
                    return true;
            }
        }

        for (int i = 1; i < 20; ++i) // Queen/Rook slide left
        {
            Index hex = new Index(enemyKingLoc.row, enemyKingLoc.col - i);
            if (!hex.IsInBounds)
                break;

            if (TryGetPiece(hex, out (Team team, Piece piece) occupier))
            {
                if (occupier.team == checkForTeam)
                {
                    Piece realPiece = GetRealPiece(occupier, promotions);
                    if (realPiece.IsRook() || realPiece == Piece.Queen)
                        return true;
                }
                break;
            }
        }

        for (int i = 1; i < 20; i++) // Queen/Rook slide right
        {
            Index hex = new Index(enemyKingLoc.row, enemyKingLoc.col + i);
            if (!hex.IsInBounds)
                break;

            if (TryGetPiece(hex, out (Team team, Piece piece) occupier))
            {
                if (occupier.team == checkForTeam)
                {
                    Piece realPiece = GetRealPiece(occupier, promotions);
                    if (realPiece.IsRook() || realPiece == Piece.Queen)
                        return true;
                }
                break;
            }
        }

        return false;
    }

    public bool HasAnyValidMoves(Team checkForTeam, List<Promotion> promotions, BoardState previousState)
    {
        return GenerateAllValidMoves(checkForTeam, promotions, previousState).Any();
    }
    public IEnumerable<(Index start, Index target, MoveType moveType, Piece promoteTo)> GenerateAllValidMoves(Team checkForTeam, List<Promotion> promotions, BoardState previousState)
    {
        Team enemyTeam = checkForTeam.Enemy();
        foreach (KeyValuePair<(Team team, Piece piece), Index> kvp in allPiecePositions)
        {
            if (kvp.Key.team != checkForTeam)
                continue;

            Piece realPiece = GetRealPiece(kvp.Key, promotions);
            var pieceMoves = MoveGenerator.GetAllPossibleMoves(kvp.Value, realPiece, kvp.Key.team, this);
            foreach ((Index target, MoveType moveType) potentialMove in pieceMoves)
            {
                if (potentialMove.moveType == MoveType.EnPassant)
                {
                    if (!potentialMove.target.TryGetNeighbor(checkForTeam == Team.White ? HexNeighborDirection.Up : HexNeighborDirection.Down, out Index enemyStartLoc))
                        continue;

                    if (IsOccupied(enemyStartLoc))
                        continue;

                    if (!previousState.TryGetPiece(enemyStartLoc, out var victim))
                        continue;

                    Piece realVictim = previousState.GetRealPiece(enemyStartLoc, promotions);
                    if (!realVictim.IsPawn())
                        continue;

                }

                // What we promote to doesn't matter for the purpose of determining enemy checks
                (BoardState newState, List<Promotion> newPromotions) = ApplyMove(kvp.Key, kvp.Value, potentialMove, promotions, Piece.Pawn1);
                if (newState.IsChecking(enemyTeam, newPromotions))
                    continue;

                if (realPiece.IsPawn() && MoveGenerator.IsPromotionRank(checkForTeam, potentialMove.target))
                {
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.Queen);
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.QueensBishop);
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.QueensRook);
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.QueensKnight);
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.BlackSquire);
                }
                else
                {
                    yield return (kvp.Value, potentialMove.target, potentialMove.moveType, Piece.Pawn1);
                }
            }
        }
    }

    public (BoardState newState, List<Promotion> promotions) ApplyMove(Index start, Index target, MoveType moveType, Piece promoteTo, List<Promotion> promotions)
    {
        return ApplyMove(allPiecePositions[start], start, (target, moveType), promotions, promoteTo);
    }

    public (BoardState newState, List<Promotion> promotions) ApplyMove((Team team, Piece piece) piece, Index startLocation, (Index target, MoveType moveType) move, List<Promotion> promotions, Piece promoteTo)
    {
        switch (move.moveType)
        {
            case MoveType.Move:
            case MoveType.Attack:
                return MoveOrAttack(piece, startLocation, move, promotions, promoteTo);
            case MoveType.Defend:
                return Defend(piece, startLocation, move, promotions);
            case MoveType.EnPassant:
                return EnPassant(piece, startLocation, move, promotions);
            default:
                throw new Exception($"Invalid move type: {move.moveType}");
        }
    }

    public readonly (BoardState newState, List<Promotion> promotions) MoveOrAttack((Team team, Piece piece) piece, Index startLocation, (Index target, MoveType moveType) move, List<Promotion> promotions, Piece promoteTo)
    {
        var newPositions = allPiecePositions.Clone();
        newPositions.Remove(startLocation);
        newPositions.Remove(move.target);
        newPositions.Add(piece, move.target);

        List<Promotion> newPromotions;
        if (!promoteTo.IsPawn() && MoveGenerator.IsPromotionRank(piece.team, move.target))
        {
            newPromotions = (promotions == null) ? new List<Promotion>(1) : new List<Promotion>(promotions);
            newPromotions.Add(new Promotion(piece.team, piece.piece, promoteTo, 1));
        }
        else
        {
            newPromotions = promotions;
        }

        var newState = new BoardState(newPositions, currentMove.Enemy(), check, checkmate, executedAtTime);
        return (newState, newPromotions);
    }
    public readonly (BoardState newState, List<Promotion> promotions) Defend((Team team, Piece piece) piece, Index startLocation, (Index target, MoveType moveType) move, List<Promotion> promotions)
    {
        var targetPiece = allPiecePositions[move.target];
        var newPositions = allPiecePositions.Clone();
        newPositions.Remove(startLocation);
        newPositions.Remove(move.target);
        newPositions.Add(piece, move.target);
        newPositions.Add(targetPiece, startLocation);

        var newState = new BoardState(newPositions, currentMove.Enemy(), check, checkmate, executedAtTime);
        return (newState, promotions);
    }
    public readonly (BoardState newState, List<Promotion> promotions) EnPassant((Team team, Piece piece) piece, Index startLocation, (Index target, MoveType moveType) move, List<Promotion> promotions)
    {
        var victimLocation = move.target.GetNeighborAt(piece.team == Team.White ? HexNeighborDirection.Down : HexNeighborDirection.Up).Value;

        var newPositions = allPiecePositions.Clone();
        newPositions.Remove(startLocation);
        newPositions.Remove(victimLocation);
        newPositions.Add(piece, move.target);

        var newState = new BoardState(newPositions, currentMove.Enemy(), check, checkmate, executedAtTime);
        return (newState, promotions);
    }

    // Json (including newtonsoft) can not properly serialize a dictionary that uses a key that is any type other than than a string.
    // To get around this, we convert our Bidirectional dictionary into a list, then serialze that list.
    public List<SerializedPiece> GetSerializeable()
    {
        List<SerializedPiece> list = new List<SerializedPiece>();
        foreach(KeyValuePair<(Team, Piece), Index> kvp in allPiecePositions)
        {
            (Team team, Piece piece) = kvp.Key;
            list.Add(new SerializedPiece{t = team, p = piece, i = kvp.Value});
        }
        return list;
    }

    public byte[] Serialize() =>
        Encoding.ASCII.GetBytes(
            JsonConvert.SerializeObject(new SerializedBoard{
                pieces = GetSerializeable(),
                currentMove = currentMove,
                check = check,
                checkmate = checkmate,
                executedAtTime = executedAtTime
            })
        );

    // When deserializing from json, because of the before mentioned dictionary issues, we must deserialize as a list, then construct our dictionary from it.
    public static BoardState GetBoardStateFromDeserializedBoard(List<SerializedPiece> list, Team currentMove, Team check, Team checkmate, float executedAtTime)
    {
        BidirectionalDictionary<(Team, Piece), Index> newDict = new BidirectionalDictionary<(Team, Piece), Index>();
        foreach(SerializedPiece tpl in list)
            newDict.Add((tpl.t, tpl.p), tpl.i);

        return new BoardState{allPiecePositions = newDict, currentMove = currentMove, check = check, checkmate = checkmate, executedAtTime = executedAtTime};
    }

    public static BoardState Deserialize(byte[] data)
    {
        string json = Encoding.ASCII.GetString(data);
        SerializedBoard boardstate = JsonConvert.DeserializeObject<SerializedBoard>(json);
        
        BidirectionalDictionary<(Team, Piece), Index> newDict = new BidirectionalDictionary<(Team, Piece), Index>();
        foreach(SerializedPiece tpl in boardstate.pieces)
            newDict.Add((tpl.t, tpl.p), tpl.i);

        return new BoardState {
            allPiecePositions = newDict,
            currentMove = boardstate.currentMove,
            check = boardstate.check,
            checkmate = boardstate.checkmate,
            executedAtTime = boardstate.executedAtTime
        };
    }

    public Piece GetRealPiece(Index index, List<Promotion> promotions)
    {
        return GetRealPiece(allPiecePositions[index], promotions);
    }
    public static Piece GetRealPiece((Team team, Piece piece) piece, List<Promotion> promotions)
    {
        if (promotions != null && piece.piece >= Piece.Pawn1)
            foreach (var promotion in promotions)
            {
                if (promotion.from == piece.piece && promotion.team == piece.team)
                {
                    return promotion.to;
                }
            }

        return piece.piece;
    }
}

[System.Serializable]
public struct SerializedBoard {
    public List<SerializedPiece> pieces;
    public Team currentMove;
    public Team check;
    public Team checkmate;
    public float executedAtTime;
}

[System.Serializable]
public struct SerializedPiece {
    public Team t;
    public Piece p;
    public Index i;
}
