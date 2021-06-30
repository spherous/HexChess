using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class IndexTests
{
    [Test]
    public void RowColConstructorTest()
    {
        var b1 = new Index(0, 0);
        Assert.AreEqual("B1", b1.GetKey());
        var d1 = new Index(0, 1);
        Assert.AreEqual("D1", d1.GetKey());
        var f1 = new Index(0, 2);
        Assert.AreEqual("F1", f1.GetKey());
        var h1 = new Index(0, 3);
        Assert.AreEqual("H1", h1.GetKey());

        var a1 = new Index(1, 0);
        Assert.AreEqual("A1", a1.GetKey());
        var c1 = new Index(1, 1);
        Assert.AreEqual("C1", c1.GetKey());
        var e1 = new Index(1, 2);
        Assert.AreEqual("E1", e1.GetKey());
        var g1 = new Index(1, 3);
        Assert.AreEqual("G1", g1.GetKey());
        var i1 = new Index(1, 4);
        Assert.AreEqual("I1", i1.GetKey());
    }
    [Test]
    public void RankAndFileConstructorTest()
    {
        var a1 = new Index(1, 'A');
        Assert.AreEqual("A1", a1.GetKey());

        foreach (byte rank in new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
            foreach (char file in new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I' })
            {
                var index = new Index(rank, file);
                Assert.AreEqual($"{file}{rank}", index.GetKey());
            }
    }

    [Test]
    public void RankAndFileConstructorRangeTest()
    {
        // file must be uppercase
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Index(1, 'a');
        });


        // Rank must be 1-10
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Index(0, 'A');
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Index(11, 'A');
        });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Index(1, 'J');
        });

        // Only valid rank 10 files are B, D, F, H
        var invalidFiles = new char[] { 'A', 'C', 'E', 'G', 'I' };
        foreach (var invalidFile in invalidFiles)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new Index(10, invalidFile);
            });
        }
    }

    [Test]
    public void InBoundsTest()
    {
        Assert.True(new Index(0, 0).IsInBounds);
        Assert.True(new Index(18, 3).IsInBounds);
        Assert.True(new Index(17, 4).IsInBounds);
    }
    [Test]
    public void OutOfBoundsTest()
    {
        Assert.False(new Index(-1, -1).IsInBounds);
        Assert.False(new Index(-1, 0).IsInBounds);
        Assert.False(new Index(0, -1).IsInBounds);
        Assert.False(new Index(18, 4).IsInBounds);
    }

    [Test]
    public void Neighbor_GetNeighborAt()
    {
        Assert.AreEqual(new Index(6, 'E'), Indexes.E5.GetNeighborAt(HexNeighborDirection.Up));
        Assert.AreEqual(new Index(6, 'F'), Indexes.E5.GetNeighborAt(HexNeighborDirection.UpRight));
        Assert.AreEqual(new Index(5, 'F'), Indexes.E5.GetNeighborAt(HexNeighborDirection.DownRight));
        Assert.AreEqual(new Index(4, 'E'), Indexes.E5.GetNeighborAt(HexNeighborDirection.Down));
        Assert.AreEqual(new Index(5, 'D'), Indexes.E5.GetNeighborAt(HexNeighborDirection.DownLeft));
        Assert.AreEqual(new Index(6, 'D'), Indexes.E5.GetNeighborAt(HexNeighborDirection.UpLeft));
    }

    [Test]
    public void Neighbor_Index()
    {
        // Bottom left, short file
        Assert.AreEqual(Indexes.A2, Indexes.A1[HexNeighborDirection.Up]);
        Assert.AreEqual(Indexes.B2, Indexes.A1[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Indexes.B1, Indexes.A1[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Index.invalid, Indexes.A1[HexNeighborDirection.Down]);
        Assert.AreEqual(Index.invalid, Indexes.A1[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Index.invalid, Indexes.A1[HexNeighborDirection.UpLeft]);

        // Top left, short file
        Assert.AreEqual(Index.invalid, Indexes.A9[HexNeighborDirection.Up]);
        Assert.AreEqual(Indexes.B10, Indexes.A9[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Indexes.B9, Indexes.A9[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Indexes.A8, Indexes.A9[HexNeighborDirection.Down]);
        Assert.AreEqual(Index.invalid, Indexes.A9[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Index.invalid, Indexes.A9[HexNeighborDirection.UpLeft]);

        // Bottom left, long file
        Assert.AreEqual(Indexes.B2, Indexes.B1[HexNeighborDirection.Up]);
        Assert.AreEqual(Indexes.C1, Indexes.B1[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Index.invalid, Indexes.B1[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Index.invalid, Indexes.B1[HexNeighborDirection.Down]);
        Assert.AreEqual(Index.invalid, Indexes.B1[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Indexes.A1, Indexes.B1[HexNeighborDirection.UpLeft]);

        // Top left, long file
        Assert.AreEqual(Index.invalid, Indexes.B10[HexNeighborDirection.Up]);
        Assert.AreEqual(Index.invalid, Indexes.B10[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Indexes.C9, Indexes.B10[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Indexes.B9, Indexes.B10[HexNeighborDirection.Down]);
        Assert.AreEqual(Indexes.A9, Indexes.B10[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Index.invalid, Indexes.B10[HexNeighborDirection.UpLeft]);

        // Bottom right, long file
        Assert.AreEqual(Indexes.H2, Indexes.H1[HexNeighborDirection.Up]);
        Assert.AreEqual(Indexes.I1, Indexes.H1[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Index.invalid, Indexes.H1[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Index.invalid, Indexes.H1[HexNeighborDirection.Down]);
        Assert.AreEqual(Index.invalid, Indexes.H1[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Indexes.G1, Indexes.H1[HexNeighborDirection.UpLeft]);

        // Top right, long file
        Assert.AreEqual(Index.invalid, Indexes.H10[HexNeighborDirection.Up]);
        Assert.AreEqual(Index.invalid, Indexes.H10[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Indexes.I9, Indexes.H10[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Indexes.H9, Indexes.H10[HexNeighborDirection.Down]);
        Assert.AreEqual(Indexes.G9, Indexes.H10[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Index.invalid, Indexes.H10[HexNeighborDirection.UpLeft]);

        // Bottom right, short file
        Assert.AreEqual(Indexes.I2, Indexes.I1[HexNeighborDirection.Up]);
        Assert.AreEqual(Index.invalid, Indexes.I1[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Index.invalid, Indexes.I1[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Index.invalid, Indexes.I1[HexNeighborDirection.Down]);
        Assert.AreEqual(Indexes.H1, Indexes.I1[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Indexes.H2, Indexes.I1[HexNeighborDirection.UpLeft]);

        // Top right, short file
        Assert.AreEqual(Index.invalid, Indexes.I9[HexNeighborDirection.Up]);
        Assert.AreEqual(Index.invalid, Indexes.I9[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Index.invalid, Indexes.I9[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Indexes.I8, Indexes.I9[HexNeighborDirection.Down]);
        Assert.AreEqual(Indexes.H9, Indexes.I9[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Indexes.H10, Indexes.I9[HexNeighborDirection.UpLeft]);
    }

    public static class Indexes
    {
        public static readonly Index A1 = new Index(1, 'A');
        public static readonly Index A2 = new Index(2, 'A');
        public static readonly Index A5 = new Index(5, 'A');
        public static readonly Index A8 = new Index(8, 'A');
        public static readonly Index A9 = new Index(9, 'A');
        public static readonly Index B1 = new Index(1, 'B');
        public static readonly Index B2 = new Index(2, 'B');
        public static readonly Index B9 = new Index(9, 'B');
        public static readonly Index B10 = new Index(10, 'B');
        public static readonly Index C1 = new Index(1, 'C');
        public static readonly Index C9 = new Index(9, 'C');
        public static readonly Index D1 = new Index(1, 'D');
        public static readonly Index E5 = new Index(5, 'E');
        public static readonly Index D10 = new Index(10, 'D');
        public static readonly Index E4 = new Index(4, 'E');
        public static readonly Index F10 = new Index(10, 'F');
        public static readonly Index G1 = new Index(1, 'G');
        public static readonly Index G9 = new Index(9, 'G');
        public static readonly Index H1 = new Index(1, 'H');
        public static readonly Index H2 = new Index(2, 'H');
        public static readonly Index H9 = new Index(9, 'H');
        public static readonly Index H10 = new Index(10, 'H');
        public static readonly Index I1 = new Index(1, 'I');
        public static readonly Index I2 = new Index(2, 'I');
        public static readonly Index I5 = new Index(5, 'I');
        public static readonly Index I9 = new Index(9, 'I');
        public static readonly Index I8 = new Index(8, 'I');
    }
}
