using NUnit.Framework;
using System.Linq;

public class BitsboardTests
{
    [Test]
    public void AllEmptyAtStartTest()
    {
        var bb = new BitsBoard();

        for (byte b = 0; b < 128; b++)
        {
            Assert.False(bb[b]);
        }
    }

    [Test]
    public void SetSingleTest()
    {
        var bb = new BitsBoard();

        bb[0] = true;
        Assert.IsTrue(bb[0]);
        Assert.IsFalse(bb[1]);

        bb[0] = false;
        Assert.IsFalse(bb[0]);
        Assert.IsFalse(bb[1]);
    }

    [Test]
    public void HighReadWriteTest()
    {
        var bb = new BitsBoard();

        bb[0] = true;
        Assert.IsTrue(bb[0]);
        Assert.IsFalse(bb[64]);
        Assert.IsFalse(bb[65]);

        bb[64] = true;
        Assert.IsTrue(bb[0]);
        Assert.IsTrue(bb[64]);
        Assert.IsFalse(bb[65]);

        bb[65] = true;
        Assert.IsTrue(bb[0]);
        Assert.IsTrue(bb[64]);
        Assert.IsTrue(bb[65]);

        bb[0] = false;
        Assert.IsFalse(bb[0]);
        Assert.IsTrue(bb[64]);
        Assert.IsTrue(bb[65]);

        bb[64] = false;
        Assert.IsFalse(bb[0]);
        Assert.IsFalse(bb[64]);
        Assert.IsTrue(bb[65]);

        bb[65] = false;
        Assert.IsFalse(bb[0]);
        Assert.IsFalse(bb[64]);
        Assert.IsFalse(bb[65]);
    }

    [Test]
    public void CanSetAllTest()
    {
        var bb = new BitsBoard();

        for (byte b = 0; b < 128; b++)
        {
            Assert.IsFalse(bb[b]);
            bb[b] = true;
            Assert.IsTrue(bb[b]);
        }
    }

    [Test]
    public void CountTest()
    {
        var bb = new BitsBoard();

        Assert.AreEqual(0, bb.Count);
        bb[0] = true;
        Assert.AreEqual(1, bb.Count);
        bb[64] = true;
        Assert.AreEqual(2, bb.Count);
        bb[0] = false;
        Assert.AreEqual(1, bb.Count);
        bb[64] = false;
        Assert.AreEqual(0, bb.Count);

        for (byte b = 0; b < 128; b++)
        {
            Assert.AreEqual(b, bb.Count);
            bb[b] = true;
            Assert.AreEqual(b + 1, bb.Count);
        }
    }

    [Test]
    public void HighCountTest()
    {
        var bb = new BitsBoard();

        Assert.AreEqual(0, bb.Count);
        bb[79] = true;
        Assert.AreEqual(1, bb.Count);
    }

    [Test]
    public void ShiftRight65()
    {
        var bb = new BitsBoard();
        bb[65] = true;
        AssertSetBits(bb, 65);

        var shifted = bb >> 65;
        AssertSetBits(shifted, 0);
    }
    [Test]
    public void ShiftLeft65()
    {
        var bb = new BitsBoard();
        bb[0] = true;
        AssertSetBits(bb, 0);

        var shifted = bb << 65;
        AssertSetBits(shifted, 65);
    }


    [Test]
    public void CountAddRemoveTest()
    {
        var bb = new BitsBoard();

        Assert.AreEqual(0, bb.Count);
        bb[0] = true;
        Assert.AreEqual(1, bb.Count);
        bb[0] = false;
        Assert.AreEqual(0, bb.Count);

        Assert.AreEqual(0, bb.Count);
        bb[80] = true;
        Assert.AreEqual(1, bb.Count);
        bb[80] = false;
        Assert.AreEqual(0, bb.Count);

        bb[20] = true;
        bb[80] = true;
        Assert.AreEqual(2, bb.Count);
    }

    [Test]
    public void CountDoubleSet()
    {
        var bb = new BitsBoard();

        Assert.AreEqual(0, bb.Count);
        bb[0] = true;
        bb[0] = true;
        Assert.AreEqual(1, bb.Count);
    }

    [Test]
    public void CountDoubleUnset()
    {
        var bb = new BitsBoard();

        Assert.AreEqual(0, bb.Count);
        bb[0] = false;
        bb[0] = false;
        Assert.AreEqual(0, bb.Count);
    }

    [Test]
    public void OrOperatorTest()
    {
        var left = new BitsBoard();
        var right = new BitsBoard();

        left[1] = true;
        left[20] = true;
        right[64] = true;
        right[80] = true;

        BitsBoard mixed = left | right;
        Assert.AreEqual(4, mixed.Count);
        Assert.IsTrue(mixed[1]);
        Assert.IsTrue(mixed[20]);
        Assert.IsTrue(mixed[64]);
        Assert.IsTrue(mixed[80]);

        Assert.IsFalse(mixed[0]);
        Assert.IsFalse(mixed[65]);
    }

    [Test]
    public void ShiftUpTest()
    {
        var index = new FastIndex(5, 'E');
        var board = new BitsBoard();

        board[index] = true;
        AssertOnlyIndexSet(board, index);

        var shifted = board.Shift(HexNeighborDirection.Up);
        AssertOnlyIndexSet(board, index);
        AssertOnlyIndexSet(shifted, new FastIndex(6, 'E'));
    }

    [Test]
    public void ShiftUpInvalidTest()
    {
        var index = new FastIndex(9, 'A');
        var board = new BitsBoard();

        board[index] = true;
        var shifted = board.Shift(HexNeighborDirection.Up);
        AssertNoIndicesSet(shifted);
    }

    [Test]
    public void ShiftDownTest()
    {
        var index = new FastIndex(5, 'E');
        var board = new BitsBoard();

        board[index] = true;
        AssertOnlyIndexSet(board, index);

        var shifted = board.Shift(HexNeighborDirection.Down);
        AssertOnlyIndexSet(board, index);
        AssertOnlyIndexSet(shifted, new FastIndex(4, 'E'));
    }

    [Test]
    public void ShiftDownAcrossBoundaryTest()
    {
        var index = FastIndex.FromByte(64);
        var expected = index[HexNeighborDirection.Down];
        var board = new BitsBoard();
        board[index] = true;

        var shifted = board.Shift(HexNeighborDirection.Down);
        AssertOnlyIndexSet(shifted, expected);
    }

    [Test]
    public void ShiftComprehensivetest([ValueSource(typeof(PrecomputedMoveData), nameof(PrecomputedMoveData.AllDirections))] HexNeighborDirection direction)
    {
        for (byte b = 0; b < 85; b++)
        {
            FastIndex index = FastIndex.FromByte(b);
            var original = new BitsBoard();
            original[index] = true;

            var expected = index[direction];
            var shifted = original.Shift(direction);
            if (expected.IsInBounds)
                AssertOnlyIndexSet(shifted, expected);
            else
                AssertNoIndicesSet(shifted);
        }
    }

    [Test]
    public void ShiftDownInvalidTest()
    {
        var index = new FastIndex(1, 'A');
        var board = new BitsBoard();

        board[index] = true;
        var shifted = board.Shift(HexNeighborDirection.Down);
        AssertNoIndicesSet(shifted);
    }

    private void AssertNoIndicesSet(BitsBoard board)
    {
        if (board.Count > 0)
        {
            System.Collections.Generic.List<string> setIndices = new System.Collections.Generic.List<string>();
            for (byte b = 0; b < 85; b++)
            {
                if (board[b])
                {
                    UnityEngine.Debug.LogError($"Byte {b} unexpectedly set");
                    setIndices.Add(FastIndex.FromByte(b).ToString());
                }
            }

            Assert.Fail($"Unexpectedly set indices: {string.Join(", ", setIndices)}");
        }
        Assert.AreEqual(0, board.Count);
    }
    private void AssertOnlyIndexSet(BitsBoard board, FastIndex expected)
    {
        if (board.Count == 0)
            Assert.Fail($"Expected {expected}, but no indices were set");

        if (!board[expected])
            Assert.Fail($"Index {expected} was not set");

        board[expected] = false;
        AssertNoIndicesSet(board);
    }


    [Test]
    public void UnpackSoloTest()
    {
        var packed = new BitsBoard();
        packed[81] = true;
        var unpacked = BitsBoard.Unpack(packed);
        Assert.AreEqual(new byte[] { 82 }, GetSetBits(unpacked));
    }

    [Test]
    public void UnpackMultiTest()
    {
        var packed = new BitsBoard();
        packed[81] = true;
        packed[82] = true;
        packed[83] = true;
        packed[84] = true;
        var unpacked = BitsBoard.Unpack(packed);
        Assert.AreEqual(new byte[] { 82, 84, 86, 88 }, GetSetBits(unpacked));
    }

    [Test]
    public void PackSoloTest()
    {
        var unpacked = new BitsBoard();
        unpacked[82] = true;
        var packed = BitsBoard.Pack(unpacked);
        Assert.AreEqual(new byte[] { 81 }, GetSetBits(packed));
    }

    [Test]
    public void PackMultiTest()
    {
        var unpacked = new BitsBoard();
        unpacked[82] = true;
        unpacked[84] = true;
        unpacked[86] = true;
        unpacked[88] = true;
        var packed = BitsBoard.Pack(unpacked);
        Assert.AreEqual(new byte[] { 81, 82, 83, 84 }, GetSetBits(packed));
    }

    private byte[] GetSetBits(BitsBoard board)
    {
        var set = new System.Collections.Generic.List<byte>();

        for (byte b = 0; b < 128; b++)
        {
            if (board[b])
                set.Add(b);
        }

        return set.ToArray();
    }

    void AssertSetBits(BitsBoard board, params byte[] expected)
    {
        var actual = GetSetBits(board);
        if (!actual.SequenceEqual(expected))
        {
            Assert.Fail($"Expected bits ({string.Join(", ", expected)}) got ({string.Join(", ", actual)})");
        }
    }
}
