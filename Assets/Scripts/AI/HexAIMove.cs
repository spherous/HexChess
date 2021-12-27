using System;
using System.Collections.Generic;
using Extensions;

public readonly struct HexAIMove
{
    public readonly static HexAIMove Invalid = new HexAIMove(Index.invalid, Index.invalid, MoveType.None);
    public readonly Index start;
    public readonly Index target;
    public readonly MoveType moveType;
    public readonly Piece promoteTo;

    public HexAIMove(Index start, Index target, MoveType moveType)
    {
        this.start = start;
        this.target = target;
        this.moveType = moveType;
        this.promoteTo = Piece.Pawn1;
    }

    public HexAIMove(Index start, Index target, MoveType moveType, Piece promoteTo)
    {
        this.start = start;
        this.target = target;
        this.moveType = moveType;
        this.promoteTo = promoteTo;
    }

    public (BoardState state, List<Promotion> promotions) Speculate(Game game)
    {
        return game.QueryMove(start, (target, moveType), game.GetCurrentBoardState(), promoteTo);
    }

    public void ApplyTo(Game game)
    {
        BoardState oldState = game.GetCurrentBoardState();

        var result = game.QueryMove(start, (target, moveType), oldState, promoteTo);

        game.SetPromotions(result.promotions);
        game.AdvanceTurn(result.newState, false, true);
    }

    public override string ToString()
    {
        return $"{moveType}({start.GetKey()} -> {target.GetKey()})";
    }

    public static IEnumerable<HexAIMove> GenerateAllValidMoves(Game game)
    {
        var state = game.turnHistory[game.turnHistory.Count - 1];
        BoardState previousState = game.turnHistory.Count > 1
            ? game.turnHistory[game.turnHistory.Count - 2]
            : default;

        foreach (var move in MoveGenerator.GenerateAllValidMoves(state.currentMove, game.promotions, state, previousState))
        {
            yield return new HexAIMove(move.start, move.target, move.moveType, move.promoteTo);
        }
    }
}
