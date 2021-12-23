#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Extensions;

public class MoveValidatorTests
{
    static readonly Team[] Teams = new Team[] { Team.White, Team.Black };

    #region IsChecking tests
    [Test]
    public void IsChecking_NoPiecesTest()
    {
        var bs = TestUtils.CreateBoardState(Array.Empty<(Index, Team, Piece)>());
        Assert.False(MoveValidator.IsChecking(Team.White, bs, null));
        Assert.False(MoveValidator.IsChecking(Team.Black, bs, null));
    }
    [Test]
    public void IsChecking_KingOnlyNoCheckTest()
    {
        var bs = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'E'), Team.White, Piece.King),
            (new Index(9, 'E'), Team.Black, Piece.King),
        });

        Assert.False(MoveValidator.IsChecking(Team.White, bs, null));
        Assert.False(MoveValidator.IsChecking(Team.Black, bs, null));
    }

    [Test]
    public void IsChecking_KingCheckEachOtherTest()
    {
        var bs = TestUtils.CreateBoardState(new[] {
            (new Index(5, 'E'), Team.White, Piece.King),
            (new Index(6, 'E'), Team.Black, Piece.King),
        });

        Assert.True(MoveValidator.IsChecking(Team.White, bs, null));
        Assert.True(MoveValidator.IsChecking(Team.Black, bs, null));
    }

    [Test]
    public void IsChecking_PawnCheckTest()
    {
        var bs = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(5, 'F'), Team.White, Piece.Pawn1),
        });

        Assert.True(MoveValidator.IsChecking(Team.White, bs, null));
        Assert.False(MoveValidator.IsChecking(Team.Black, bs, null));
    }
    
    [Test]
    public void IsChecking_BishopCheckTest()
    {
        var bs1 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(7, 'A'), Team.White, Piece.KingsBishop),
        });
        Assert.True(MoveValidator.IsChecking(Team.White, bs1, null));

        var bs2 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(7, 'A'), Team.White, Piece.KingsBishop),
            (new Index(7, 'B'), Team.White, Piece.KingsRook), // blocking bishop
        });
        Assert.False(MoveValidator.IsChecking(Team.White, bs2, null));
    }

    [Test]
    public void IsChecking_RookCheckTest()
    {
        var bs1 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(5, 'A'), Team.White, Piece.KingsRook),
        });
        Assert.True(MoveValidator.IsChecking(Team.White, bs1, null));

        var bs2 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(5, 'B'), Team.White, Piece.KingsRook),
        });
        Assert.False(MoveValidator.IsChecking(Team.White, bs2, null));

        var bs3 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(5, 'A'), Team.White, Piece.KingsRook),
            (new Index(5, 'C'), Team.White, Piece.Pawn5), // blocks rook
        });
        Assert.False(MoveValidator.IsChecking(Team.White, bs3, null));
    }
    [Test]
    public void IsChecking_KnightCheckTest()
    {
        var bs1 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(5, 'H'), Team.White, Piece.KingsKnight),
        });
        Assert.True(MoveValidator.IsChecking(Team.White, bs1, null));

        var bs2 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(4, 'H'), Team.White, Piece.KingsKnight),
        });
        Assert.False(MoveValidator.IsChecking(Team.White, bs2, null));

        var bs3 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(5, 'H'), Team.Black, Piece.KingsKnight), // Knight on same team as king
        });
        Assert.False(MoveValidator.IsChecking(Team.White, bs3, null));

    }
    [Test]
    public void IsChecking_SquireCheckTest()
    {
        var bs1 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(4, 'F'), Team.White, Piece.WhiteSquire),
        });
        Assert.True(MoveValidator.IsChecking(Team.White, bs1, null));

        var bs2 = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(4, 'G'), Team.White, Piece.WhiteSquire),
        });
        Assert.False(MoveValidator.IsChecking(Team.White, bs2, null));
    }

    [Test]
    public void IsChecking_PawnPromotionCheckTest()
    {
        var bs = TestUtils.CreateBoardState(new[] {
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(5, 'E'), Team.Black, Piece.King),
            (new Index(1, 'E'), Team.White, Piece.Pawn1),
        });

        Assert.False(MoveValidator.IsChecking(Team.Black, bs, null));
        Assert.False(MoveValidator.IsChecking(Team.White, bs, null));
        Assert.True(MoveValidator.IsChecking(Team.White, bs, new List<Promotion>() { new Promotion(Team.White, Piece.Pawn1, Piece.Queen, 0) }));
        Assert.False(MoveValidator.IsChecking(Team.Black, bs, new List<Promotion>() { new Promotion(Team.White, Piece.Pawn1, Piece.Queen, 0) }));
    }
    #endregion

    #region HasAnyValidMoves tests
    [Test]
    public void AnyValid_EmptyBoardTest()
    {
        var board1 = TestUtils.CreateBoardState(null);
        Assert.False(MoveValidator.HasAnyValidMoves(Team.White, null, board1, default));
        Assert.False(MoveValidator.HasAnyValidMoves(Team.Black, null, board1, default));

        var board2 = TestUtils.CreateBoardState(new[] {
            (new Index(5, 'E'), Team.White, Piece.King),
        });

        Assert.True(MoveValidator.HasAnyValidMoves(Team.White, null, board2, default));
        Assert.False(MoveValidator.HasAnyValidMoves(Team.Black, null, board2, default));

        var board3 = TestUtils.CreateBoardState(new[] {
            (new Index(5, 'E'), Team.Black, Piece.King),
        });

        Assert.False(MoveValidator.HasAnyValidMoves(Team.White, null, board3, default));
        Assert.True(MoveValidator.HasAnyValidMoves(Team.Black, null, board3, default));
    }

    [Test]
    public void AnyValid_StalemateTest([ValueSource(nameof(Teams))] Team attacker)
    {
        Team defender = attacker.Enemy();
        var bs = TestUtils.CreateBoardState(defender, new[] {
            (new Index(9, 'A'), attacker, Piece.King),
            (new Index(3, 'B'), attacker, Piece.Queen),
            (new Index(1, 'A'), defender, Piece.King),
        });
        Assert.False(MoveValidator.HasAnyValidMoves(defender, null, bs, default));
    }

    #endregion

}
