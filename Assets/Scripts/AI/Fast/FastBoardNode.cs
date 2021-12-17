using System;
using System.Collections.Generic;
using Extensions;

sealed public class FastBoardNode
{
    public static bool newstuff;

    public Team currentMove = Team.None;
    public FastIndex whiteKing = FastIndex.Invalid;
    public FastIndex blackKing = FastIndex.Invalid;
    public FastIndex passantableIndex = FastIndex.Invalid;
    public int plySincePawnMovedOrPieceTaken = 0;
    public readonly (Team team, FastPiece piece)[] positions = new (Team team, FastPiece piece)[85];

    private readonly Stack<(Team team, FastPiece piece)> captureHistory = new Stack<(Team team, FastPiece piece)>(10);
    private readonly Stack<int> fiftyMoveRuleHistory = new Stack<int>(10);
    private readonly Stack<FastIndex> passantableHistory = new Stack<FastIndex>(10);

    public FastBoardNode(BoardState state, List<Promotion> promotions)
    {
        currentMove = state.currentMove;

        foreach (KeyValuePair<(Team team, Piece piece), Index> kvp in state.allPiecePositions)
        {
            var index = (FastIndex)kvp.Value;
            FastPiece realPiece = HexachessagonEngine.GetRealPiece(kvp.Key, promotions).ToFastPiece();
            positions[kvp.Value.ToByte()] = (kvp.Key.Item1, realPiece);

            if (realPiece == FastPiece.King)
            {
                if (kvp.Key.team == Team.White)
                    whiteKing = index;
                else
                    blackKing = index;
            }
        }
    }
    
    public FastBoardNode(Game game) : this(game.GetCurrentBoardState(), game.promotions){}

    public FastBoardNode()
    {
    }

    /// <summary>
    /// Is a piece from <paramref name="attacker"/> attacking the enemy king?
    /// </summary>
    /// <param name="attacker"></param>
    /// <returns>true if the enemy king is threatened</returns>
    public bool IsChecking(Team attacker)
    {
        Team defender = attacker.Enemy();
        FastIndex defenderKingLoc = defender == Team.White ? whiteKing : blackKing;

        // Rook/Queen checks
        var rookRays = PrecomputedMoveData.rookRays[defenderKingLoc.ToByte()];
        foreach (var rookRay in rookRays)
        {
            if (IsCheckingRay(defender, rookRay, FastPiece.Rook))
                return true;
        }

        // Bishop/Queen checks
        var bishopRays = PrecomputedMoveData.bishopRays[defenderKingLoc.ToByte()];
        foreach (var bishopRay in bishopRays)
        {
            if (IsCheckingRay(defender, bishopRay, FastPiece.Bishop))
                return true;
        }

        // Squire checks
        foreach (var possibleMove in PrecomputedMoveData.squireMoves[defenderKingLoc.ToByte()])
        {
            if (TryGetPiece(possibleMove, out (Team team, FastPiece piece) occupier) && occupier.team == attacker && occupier.piece == FastPiece.Squire)
                return true;
        }

        // Knight checks
        foreach (var possibleMove in PrecomputedMoveData.knightMoves[defenderKingLoc.ToByte()])
        {
            if (TryGetPiece(possibleMove, out (Team team, FastPiece piece) occupier) && occupier.team == attacker && occupier.piece == FastPiece.Knight)
                return true;
        }

        // Pawn & King checks
        foreach (var direction in PrecomputedMoveData.AllDirections)
        {
            var index = defenderKingLoc[direction];
            if (!index.IsInBounds)
                continue;

            var occupant = this[index];
            if (occupant.team != attacker)
                continue;

            if (occupant.piece == FastPiece.King)
                return true;

            if (occupant.piece == FastPiece.Pawn)
            {
                if (attacker == Team.White)
                {
                    if (direction == HexNeighborDirection.DownLeft || direction == HexNeighborDirection.DownRight)
                        return true;
                }
                else
                {
                    if (direction == HexNeighborDirection.UpLeft || direction == HexNeighborDirection.UpRight)
                        return true;
                }
            }
        }

        return false;
    }

    bool IsCheckingRay(Team kingTeam, FastIndex[] ray, FastPiece rayPiece)
    {
        foreach (var index in ray)
        {
            var occupant = this[index];
            if (occupant.team == kingTeam)
                break;
            if (occupant.team == Team.None)
                continue;

            return occupant.piece == rayPiece || occupant.piece == FastPiece.Queen;
        }
        return false;
    }

    #region Move doing
    public void DoMove(FastMove move)
    {
        fiftyMoveRuleHistory.Push(plySincePawnMovedOrPieceTaken);
        passantableHistory.Push(passantableIndex);
        passantableIndex = FastIndex.Invalid;

        switch (move.moveType)
        {
            case MoveType.Move:
                if (this[move.start].piece == FastPiece.Pawn)
                {
                    plySincePawnMovedOrPieceTaken = 0;
                    bool isDoubleMove = Math.Abs(move.target.HexId - move.start.HexId) == 18;
                    if (isDoubleMove)
                        passantableIndex = move.target;
                }
                else
                    plySincePawnMovedOrPieceTaken++;

                DoMoveOrAttack(move);
                break;
            case MoveType.Attack:
                plySincePawnMovedOrPieceTaken = 0;
                DoMoveOrAttack(move);
                break;
            case MoveType.Defend:
                plySincePawnMovedOrPieceTaken++;
                DoDefend(move);
                break;
            case MoveType.EnPassant:
                plySincePawnMovedOrPieceTaken = 0;
                DoEnPassant(move);
                break;
            default:
                throw new Exception($"Invalid move type: {move.moveType}");
        }
        currentMove = currentMove.Enemy();
    }

    private void DoMoveOrAttack(FastMove move)
    {
        var piece = this[move.start];
        var victim = this[move.target];

        if (victim.team != Team.None && victim.piece == FastPiece.King)
            throw new ArgumentException($"Cannot apply move {move} because it would capture the {victim.team} {victim.piece}");

        this[move.start] = default;
        this[move.target] = move.promoteTo == FastPiece.Pawn ? piece : (piece.team, move.promoteTo);

        if (piece.piece == FastPiece.King)
        {
            if (piece.team == Team.White)
                whiteKing = move.target;
            else
                blackKing = move.target;
        }

        captureHistory.Push(victim);
    }
    private void DoDefend(FastMove move)
    {
        byte position1 = move.start.HexId;
        byte position2 = move.target.HexId;
        var piece1 = this[position1];
        var piece2 = this[position2];
        this[position1] = piece2;
        this[position2] = piece1;

        if (whiteKing == move.target)
            whiteKing = move.start;
        else if (blackKing == move.target)
            blackKing = move.start;
    }
    private void DoEnPassant(FastMove move)
    {
        var attacker = this[move.start];
        var victimLocation = move.target.GetNeighborAt(attacker.team == Team.White ? HexNeighborDirection.Down : HexNeighborDirection.Up).Value;

        var victim = this[victimLocation];

        if (victim.team == Team.None)
            throw new ArgumentException($"Cannot apply move {move} because target is empty");

        if (victim.team != Team.None && victim.piece == FastPiece.King)
            throw new ArgumentException($"Cannot apply move {move} because it would capture the {victim.team} {victim.piece}");

        this[move.start] = default;
        this[victimLocation] = default;
        this[move.target] = attacker;
    }
    #endregion

    #region Move Undoing
    public void UndoMove(FastMove move)
    {
        switch (move.moveType)
        {
            case MoveType.Move:
            case MoveType.Attack:
                UndoMoveOrAttack(move);
                break;
            case MoveType.Defend:
                UndoDefend(move);
                break;
            case MoveType.EnPassant:
                UndoEnPassant(move);
                break;
            default:
                throw new Exception($"Invalid move type: {move.moveType}");
        }
        currentMove = currentMove.Enemy();
        plySincePawnMovedOrPieceTaken = fiftyMoveRuleHistory.Pop();
        passantableIndex = passantableHistory.Pop();
    }
    private void UndoMoveOrAttack(FastMove move)
    {
        var piece = this[move.target];

        this[move.target] = captureHistory.Pop();
        this[move.start] = move.promoteTo == FastPiece.Pawn ? piece : (piece.team, FastPiece.Pawn);

        if (piece.piece == FastPiece.King)
        {
            if (piece.team == Team.White)
                whiteKing = move.start;
            else
                blackKing = move.start;
        }
    }
    private void UndoDefend(FastMove move)
    {
        byte position1 = move.start.ToByte();
        byte position2 = move.target.ToByte();
        var piece1 = this[position1];
        var piece2 = this[position2];
        this[position1] = piece2;
        this[position2] = piece1;

        if (whiteKing == move.start)
            whiteKing = move.target;
        else if (whiteKing == move.target)
            whiteKing = move.start;
        else if (blackKing == move.start)
            blackKing = move.target;
        else if (blackKing == move.target)
            blackKing = move.start;
    }
    private void UndoEnPassant(FastMove move)
    {
        var attacker = this[move.target];
        var victimLocation = move.target.GetNeighborAt(attacker.team == Team.White ? HexNeighborDirection.Down : HexNeighborDirection.Up).Value;

        this[move.start] = attacker;
        this[move.target] = default;
        this[victimLocation] = (attacker.team.Enemy(), FastPiece.Pawn);
    }

    #endregion

    public (Team team, FastPiece piece) this[Index index]
    {
        get => positions[index.ToByte()];
        set { this[index.ToByte()] = value; }
    }
    public (Team team, FastPiece piece) this[FastIndex index]
    {
        get => positions[index.HexId];
        set { this[index.HexId] = value; }
    }
    public (Team team, FastPiece piece) this[byte index]
    {
        get => positions[index];
        set { positions[index] = value; }
    }

    public bool TryGetPiece(FastIndex index, out (Team team, FastPiece piece) piece)
    {
        piece = positions[index.ToByte()];
        return piece.team != Team.None;
    }
    public bool TryGetPiece(Index index, out (Team team, FastPiece piece) piece)
    {
        piece = positions[index.ToByte()];
        return piece.team != Team.None;
    }

    public bool IsOccupied(FastIndex value)
    {
        return positions[value.ToByte()].team != Team.None;
    }
    public bool IsOccupied(Index value)
    {
        return positions[value.ToByte()].team != Team.None;
    }


    #region Move generation and validation
    public IEnumerable<FastMove> GetAllValidMoves()
    {
        return GetAllValidMoves(currentMove);
    }
    public IEnumerable<FastMove> GetAllValidMoves(Team team)
    {
        foreach (var possibleMove in GetAllPossibleMoves(team))
        {
            if (IsMoveValid(possibleMove))
                yield return possibleMove;
        }
    }
    public bool HasAnyValidMoves(Team team)
    {
        foreach (var move in GetAllValidMoves(team))
            return true;

        return false;
    }

    private bool IsMoveValid(FastMove move)
    {
        DoMove(move);
        try
        {
            return !IsChecking(currentMove);
        }
        finally
        {
            UndoMove(move);
        }
    }

    public List<FastMove> GetAllPossibleMoves()
    {
        return GetAllPossibleMoves(currentMove);
    }
    public List<FastMove> GetAllPossibleMoves(Team team)
    {
        var possibleMoves = new List<FastMove>();
        AddAllPossibleMoves(possibleMoves, team);
        return possibleMoves;
    }

    public void AddAllPossibleMoves(List<FastMove> moves, Team team, bool generateQuiet = true)
    {
        for (byte i = 0; i < positions.Length; i++)
        {
            var piece = positions[i];
            if (piece.team != team)
                continue;

            FastIndex index = FastIndex.FromByte(i);
            FastPossibleMoveGenerator.AddAllPossibleMoves(moves, index, piece.piece, team, this, generateQuiet);
        }
    }

    #endregion
}
