using System;
using System.Collections.Generic;
using NUnit.Framework;

public class HexachessagonEngineTests
{
    #region ApplyMove tests
    [Test]
    public void ApplyMove_MoveTest()
    {
        var bs = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(1, 'E'), Team.White, Piece.Pawn1),
        });

        (Team, Piece) piece = (Team.White, Piece.Pawn1);
        (Index target, MoveType moveType) move = (new Index(2, 'E'), MoveType.Move);
        (BoardState newState, List<Promotion> promotions) = HexachessagonEngine.QueryMove(new Index(1, 'E'), move, bs, Piece.Pawn1, null);

        Assert.Null(promotions);
        Assert.True(newState.IsOccupiedBy(move.target, piece));
        Assert.False(newState.IsOccupied(new Index(1, 'E')));
    }
    [Test]
    public void ApplyMove_PromotionTest()
    {
        var bs = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(8, 'E'), Team.White, Piece.Pawn1),
        });

        (BoardState newState, List<Promotion> promotions) = HexachessagonEngine.QueryMove(new Index(8, 'E'), (new Index(9, 'E'), MoveType.Move), bs, Piece.Queen, null);

        Assert.AreEqual(promotions[0], new Promotion(Team.White, Piece.Pawn1, Piece.Queen, 1));
        Assert.AreEqual(1, promotions.Count);

        Assert.NotNull(promotions);
        Assert.True(newState.IsOccupiedBy(new Index(9, 'E'), (Team.White, Piece.Pawn1)));
        Assert.False(newState.IsOccupied(new Index(8, 'E')));
    }

    [Test]
    public void ApplyMove_AttackTest()
    {
        Index victimLocation = new Index(5, 'E');
        Index attackerLocation = new Index(1, 'E');

        var bs = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(9, 'I'), Team.Black, Piece.King),
            (attackerLocation, Team.White, Piece.KingsRook),
            (victimLocation, Team.Black, Piece.Pawn1),
        });

        (Index target, MoveType moveType) move = (victimLocation, MoveType.Attack);
        var attacker = bs.allPiecePositions[attackerLocation];
        (BoardState newState, List<Promotion> promotions) = HexachessagonEngine.QueryMove(attackerLocation, move, bs, Piece.Pawn1, null);

        Assert.Null(promotions);
        Assert.True(newState.IsOccupiedBy(move.target, attacker));
        Assert.False(newState.IsOccupied(attackerLocation));
    }

    [Test]
    public void ApplyMove_DefendTest()
    {
        Index victimLocation = new Index(2, 'E');
        Index defenderLocation = new Index(1, 'E');

        var bs = TestUtils.CreateBoardState(new[] {
            (new Index(9, 'I'), Team.Black, Piece.King),
            (victimLocation, Team.White, Piece.King),
            (defenderLocation, Team.White, Piece.KingsRook),
        });

        (Index target, MoveType moveType) move = (victimLocation, MoveType.Defend);
        var defender = bs.allPiecePositions[defenderLocation];
        var victim = bs.allPiecePositions[victimLocation];

        (BoardState newState, List<Promotion> promotions) = HexachessagonEngine.QueryMove(defenderLocation, move, bs, Piece.Pawn1, null);

        Assert.Null(promotions);
        Assert.True(newState.IsOccupiedBy(victimLocation, defender));
        Assert.True(newState.IsOccupiedBy(defenderLocation, victim));
    }

    [Test]
    public void ApplyMove_EnPassantTest()
    {
        Index attackerLocation = new Index(6, 'B');
        Index victimLocation = new Index(6, 'A');

        var bs = TestUtils.CreateBoardState(new[] {
            (new Index(9, 'I'), Team.Black, Piece.King),
            (new Index(1, 'A'), Team.White, Piece.King),
            (victimLocation, Team.Black, Piece.Pawn1),
            (attackerLocation, Team.White, Piece.Pawn1),
        });

        (Index target, MoveType moveType) move = (victimLocation.GetNeighborAt(HexNeighborDirection.Up)!.Value, MoveType.EnPassant);
        var victim = bs.allPiecePositions[victimLocation];
        var attacker = bs.allPiecePositions[attackerLocation];
        (BoardState newState, List<Promotion> promotions) = HexachessagonEngine.QueryMove(attackerLocation, move, bs, Piece.Pawn1, null);

        Assert.Null(promotions);
        Assert.False(newState.IsOccupied(attackerLocation));
        Assert.False(newState.IsOccupied(victimLocation));
        Assert.True(newState.IsOccupiedBy(move.target, attacker));
    }
    #endregion
}
