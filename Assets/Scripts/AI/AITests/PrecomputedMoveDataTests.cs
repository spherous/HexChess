using System;
using NUnit.Framework;

public class PrecomputedMoveDataTests
{
    [Test]
    public void KnightMovesTopLeft()
    {
        // Start at B10
        // Expect E9, D8, C7, A7
        var moves = PrecomputedMoveData.knightMoves[new Index(10, 'B').ToByte()];
        Assert.That(moves, Is.EquivalentTo(new FastIndex[] {
            new FastIndex(9, 'E'),
            new FastIndex(8, 'D'),
            new FastIndex(7, 'C'),
            new FastIndex(7, 'A')
        }));
    }
    [Test]
    public void KnightMovesBottomMiddle()
    {
        var moves = PrecomputedMoveData.knightMoves[new Index(1, 'E').ToByte()];
        Assert.That(moves, Is.EquivalentTo(new FastIndex[] {
            new FastIndex(1, 'B'),
            new FastIndex(2, 'B'),
            new FastIndex(3, 'C'),
            new FastIndex(4, 'D'),
            new FastIndex(4, 'F'),
            new FastIndex(3, 'G'),
            new FastIndex(2, 'H'),
            new FastIndex(1, 'H'),
        }));
    }
    [Test]
    public void KnightMoveBidirectional()
    {
        AssertBidirectional(PrecomputedMoveData.knightMoves);
    }

    [Test]
    public void SquireMoveBidirectional()
    {
        AssertBidirectional(PrecomputedMoveData.squireMoves);
    }

    void AssertBidirectional(FastIndex[][] moves)
    {
        for (byte b = 0; b < moves.Length; b++)
        {
            FastIndex start = FastIndex.FromByte(b);

            foreach (var target in moves[b])
            {
                var fromTarget = moves[target.HexId];
                if (Array.IndexOf(fromTarget, start) < 0)
                {
                    Assert.Fail($"{start} can attack {target}, but {target} cannot attack {start}");
                }
            }
        }
    }
}
