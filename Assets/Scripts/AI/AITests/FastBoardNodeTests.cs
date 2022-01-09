using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Extensions;

public class FastBoardNodeTests
{
    static readonly Team[] Teams = new Team[] { Team.White, Team.Black };

    public FastBoardNode CreateBoardNode(Team toMove, (Team team, Piece piece, Index location)[] pieces)
    {
        var piecePositions = new BidirectionalDictionary<(Team team, Piece piece), Index>();
        foreach (var piece in pieces)
            piecePositions.Add((piece.team, piece.piece), piece.location);

        var state = new BoardState(piecePositions, toMove, Team.None, Team.None, 0);
        return new FastBoardNode(state, null);
    }

    [Test]
    public void IsChecking_QueenAttack([ValueSource(nameof(Teams))] Team attacker)
    {
        Team defender = attacker.Enemy();

        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(9, 'A')),
            (defender, Piece.King, new Index(1, 'G')),
            (attacker, Piece.Queen, new Index(3, 'C')),
        });

        Assert.IsTrue(board.IsChecking(attacker));
        Assert.IsFalse(board.IsChecking(defender));
    }

    [Test]
    public void IsChecking_QueenAttack_DefenderBlock([ValueSource(nameof(Teams))] Team attacker)
    {
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(9, 'A')),
            (defender, Piece.King, new Index(1, 'G')),
            (attacker, Piece.Queen, new Index(3, 'C')),
            (defender, Piece.Pawn1, new Index(2, 'E')),
        });

        Assert.IsFalse(board.IsChecking(attacker));
        Assert.IsFalse(board.IsChecking(defender));

        var moves = board.GetAllPossibleMoves();
        Assert.That(moves, Has.Member(new FastMove(new Index(3, 'C'), new Index(2, 'E'), MoveType.Attack)));
        Assert.That(moves, Has.No.Member(new FastMove(new Index(3, 'C'), new Index(1, 'G'), MoveType.Attack)));
    }

    [Test]
    public void IsChecking_QueenAttack_AttackerBlock([ValueSource(nameof(Teams))] Team attacker)
    {
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(9, 'A')),
            (defender, Piece.King, new Index(1, 'G')),
            (attacker, Piece.Queen, new Index(3, 'C')),
            (attacker, Piece.Pawn1, new Index(2, 'E')),
        });

        Assert.IsFalse(board.IsChecking(attacker));
        Assert.IsFalse(board.IsChecking(defender));

        var moves = board.GetAllPossibleMoves();
        Assert.That(moves, Has.No.Member(new FastMove(new Index(3, 'C'), new Index(2, 'E'), MoveType.Attack)));
        Assert.That(moves, Has.No.Member(new FastMove(new Index(3, 'C'), new Index(2, 'E'), MoveType.Move)));
        Assert.That(moves, Has.No.Member(new FastMove(new Index(3, 'C'), new Index(1, 'G'), MoveType.Attack)));
    }

    [Test]
    public void IsChecking_KnightAttack_Middle([ValueSource(nameof(Teams))] Team attacker)
    {
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(9, 'A')),
            (defender, Piece.King, new Index(5, 'E')),

            (attacker, Piece.KingsKnight, new Index(3, 'D')),
        });

        Assert.True(board.IsChecking(attacker));
        Assert.IsFalse(board.IsChecking(defender));
    }

    [Test]
    public void IsChecking_KnightAttack_Bottom([ValueSource(nameof(Teams))] Team attacker)
    {
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(9, 'A')),
            (defender, Piece.King, new Index(1, 'G')),

            (attacker, Piece.KingsKnight, new Index(1, 'D')),
        });

        Assert.True(board.IsChecking(attacker));
        Assert.IsFalse(board.IsChecking(defender));
    }

    [Test]
    public void IsChecking_SquireAttack_Middle([ValueSource(nameof(Teams))] Team attacker)
    {
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(9, 'A')),
            (defender, Piece.King, new Index(5, 'E')),

            (attacker, Piece.GraySquire, new Index(5, 'C')),
        });

        Assert.True(board.IsChecking(attacker));
        Assert.IsFalse(board.IsChecking(defender));
    }

    [Test]
    public void IsChecking_SquireAttack_Bottom([ValueSource(nameof(Teams))] Team attacker)
    {
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(9, 'A')),
            (defender, Piece.King, new Index(1, 'H')),

            (attacker, Piece.GraySquire, new Index(1, 'F')),
        });

        Assert.True(board.IsChecking(attacker));
        Assert.IsFalse(board.IsChecking(defender));
    }
    [Test]
    public void IsChecking_SquireAttack_Top([ValueSource(nameof(Teams))] Team attacker)
    {
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(9, 'A')),
            (defender, Piece.King, new Index(10, 'H')),

            (attacker, Piece.GraySquire, new Index(10, 'F')),
        });

        Assert.True(board.IsChecking(attacker));
        Assert.IsFalse(board.IsChecking(defender));
    }
}
