using NUnit.Framework;

public class FastIndexTests
{
    [Test]
    public void ToByteTest()
    {
        Assert.AreEqual(0, Indexes.A1.ToByte());
        Assert.AreEqual(40, Indexes.E5.ToByte());
        Assert.AreEqual(80, Indexes.I9.ToByte());
        Assert.AreEqual(81, Indexes.B10.ToByte());
        Assert.AreEqual(82, Indexes.D10.ToByte());
        Assert.AreEqual(83, Indexes.F10.ToByte());
        Assert.AreEqual(84, Indexes.H10.ToByte());
    }
    [Test]
    public void FromByteTest()
    {
        Assert.AreEqual(Indexes.A1, FastIndex.FromByte(0));
        Assert.AreEqual(Indexes.E5, FastIndex.FromByte(40));
        Assert.AreEqual(Indexes.I9, FastIndex.FromByte(80));
        Assert.AreEqual(Indexes.B10, FastIndex.FromByte(81));
        Assert.AreEqual(Indexes.D10, FastIndex.FromByte(82));
        Assert.AreEqual(Indexes.F10, FastIndex.FromByte(83));
        Assert.AreEqual(Indexes.H10, FastIndex.FromByte(84));
    }

    [Test]
    public void IndexByteAllValuesTest()
    {
        for (byte i = 0; i < 85; i++)
        {
            FastIndex index = FastIndex.FromByte(i);
            Assert.AreEqual(i, index.ToByte());
        }
    }

    [Test]
    public void Neighbor_GetNeighborAt()
    {
        Assert.AreEqual(new FastIndex(6, 'E'), Indexes.E5.GetNeighborAt(HexNeighborDirection.Up));
        Assert.AreEqual(new FastIndex(6, 'F'), Indexes.E5.GetNeighborAt(HexNeighborDirection.UpRight));
        Assert.AreEqual(new FastIndex(5, 'F'), Indexes.E5.GetNeighborAt(HexNeighborDirection.DownRight));
        Assert.AreEqual(new FastIndex(4, 'E'), Indexes.E5.GetNeighborAt(HexNeighborDirection.Down));
        Assert.AreEqual(new FastIndex(5, 'D'), Indexes.E5.GetNeighborAt(HexNeighborDirection.DownLeft));
        Assert.AreEqual(new FastIndex(6, 'D'), Indexes.E5.GetNeighborAt(HexNeighborDirection.UpLeft));
    }


    [Test]
    public void Neighbor_TryGetNeighbor_Self()
    {
        FastIndex hex = Indexes.A1;
        hex.TryGetNeighbor(HexNeighborDirection.Up, out hex);
        Assert.AreEqual(Indexes.A2, hex);
    }

    [Test]
    public void Neighbor_Index()
    {
        // Bottom left, short file
        Assert.AreEqual(Indexes.A2, Indexes.A1[HexNeighborDirection.Up]);
        Assert.AreEqual(Indexes.B2, Indexes.A1[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Indexes.B1, Indexes.A1[HexNeighborDirection.DownRight]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.A1[HexNeighborDirection.Down]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.A1[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.A1[HexNeighborDirection.UpLeft]);

        // Top left, short file
        Assert.AreEqual(FastIndex.Invalid, Indexes.A9[HexNeighborDirection.Up]);
        Assert.AreEqual(Indexes.B10, Indexes.A9[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Indexes.B9, Indexes.A9[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Indexes.A8, Indexes.A9[HexNeighborDirection.Down]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.A9[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.A9[HexNeighborDirection.UpLeft]);

        // Bottom left, long file
        Assert.AreEqual(Indexes.B2, Indexes.B1[HexNeighborDirection.Up]);
        Assert.AreEqual(Indexes.C1, Indexes.B1[HexNeighborDirection.UpRight]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.B1[HexNeighborDirection.DownRight]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.B1[HexNeighborDirection.Down]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.B1[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Indexes.A1, Indexes.B1[HexNeighborDirection.UpLeft]);

        // Top left, long file
        Assert.AreEqual(FastIndex.Invalid, Indexes.B10[HexNeighborDirection.Up]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.B10[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Indexes.C9, Indexes.B10[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Indexes.B9, Indexes.B10[HexNeighborDirection.Down]);
        Assert.AreEqual(Indexes.A9, Indexes.B10[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.B10[HexNeighborDirection.UpLeft]);

        // Bottom right, long file
        Assert.AreEqual(Indexes.H2, Indexes.H1[HexNeighborDirection.Up]);
        Assert.AreEqual(Indexes.I1, Indexes.H1[HexNeighborDirection.UpRight]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.H1[HexNeighborDirection.DownRight]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.H1[HexNeighborDirection.Down]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.H1[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Indexes.G1, Indexes.H1[HexNeighborDirection.UpLeft]);

        // Top right, long file
        Assert.AreEqual(FastIndex.Invalid, Indexes.H10[HexNeighborDirection.Up]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.H10[HexNeighborDirection.UpRight]);
        Assert.AreEqual(Indexes.I9, Indexes.H10[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Indexes.H9, Indexes.H10[HexNeighborDirection.Down]);
        Assert.AreEqual(Indexes.G9, Indexes.H10[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.H10[HexNeighborDirection.UpLeft]);

        // Bottom right, short file
        Assert.AreEqual(Indexes.I2, Indexes.I1[HexNeighborDirection.Up]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.I1[HexNeighborDirection.UpRight]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.I1[HexNeighborDirection.DownRight]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.I1[HexNeighborDirection.Down]);
        Assert.AreEqual(Indexes.H1, Indexes.I1[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Indexes.H2, Indexes.I1[HexNeighborDirection.UpLeft]);

        // Top right, short file
        Assert.AreEqual(FastIndex.Invalid, Indexes.I9[HexNeighborDirection.Up]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.I9[HexNeighborDirection.UpRight]);
        Assert.AreEqual(FastIndex.Invalid, Indexes.I9[HexNeighborDirection.DownRight]);
        Assert.AreEqual(Indexes.I8, Indexes.I9[HexNeighborDirection.Down]);
        Assert.AreEqual(Indexes.H9, Indexes.I9[HexNeighborDirection.DownLeft]);
        Assert.AreEqual(Indexes.H10, Indexes.I9[HexNeighborDirection.UpLeft]);
    }

    [Test]
    public void MirrorTests()
    {
        Assert.AreEqual(Indexes.E5, Indexes.E5.Mirror());

        Assert.AreEqual(Indexes.A1, Indexes.A9.Mirror());
        Assert.AreEqual(Indexes.A2, Indexes.A8.Mirror());
        Assert.AreEqual(Indexes.A5, Indexes.A5.Mirror());
        Assert.AreEqual(Indexes.A8, Indexes.A2.Mirror());
        Assert.AreEqual(Indexes.A9, Indexes.A1.Mirror());

        Assert.AreEqual(Indexes.B1, Indexes.B10.Mirror());
        Assert.AreEqual(Indexes.B2, Indexes.B9.Mirror());
        Assert.AreEqual(Indexes.B5, Indexes.B6.Mirror());
        Assert.AreEqual(Indexes.B6, Indexes.B5.Mirror());
        Assert.AreEqual(Indexes.B9, Indexes.B2.Mirror());
        Assert.AreEqual(Indexes.B10, Indexes.B1.Mirror());

        Assert.AreEqual(Indexes.H1, Indexes.H10.Mirror());
        Assert.AreEqual(Indexes.H2, Indexes.H9.Mirror());
        Assert.AreEqual(Indexes.H5, Indexes.H6.Mirror());
        Assert.AreEqual(Indexes.H6, Indexes.H5.Mirror());
        Assert.AreEqual(Indexes.H9, Indexes.H2.Mirror());
        Assert.AreEqual(Indexes.H10, Indexes.H1.Mirror());

        Assert.AreEqual(Indexes.I1, Indexes.I9.Mirror());
        Assert.AreEqual(Indexes.I2, Indexes.I8.Mirror());
        Assert.AreEqual(Indexes.I5, Indexes.I5.Mirror());
        Assert.AreEqual(Indexes.I8, Indexes.I2.Mirror());
        Assert.AreEqual(Indexes.I9, Indexes.I1.Mirror());
    }

    public static class Indexes
    {
        public static readonly FastIndex A1 = new FastIndex(new Index(1, 'A'));
        public static readonly FastIndex A2 = new FastIndex(new Index(2, 'A'));
        public static readonly FastIndex A5 = new FastIndex(new Index(5, 'A'));
        public static readonly FastIndex A8 = new FastIndex(new Index(8, 'A'));
        public static readonly FastIndex A9 = new FastIndex(new Index(9, 'A'));
        public static readonly FastIndex B1 = new FastIndex(new Index(1, 'B'));
        public static readonly FastIndex B2 = new FastIndex(new Index(2, 'B'));
        public static readonly FastIndex B5 = new FastIndex(new Index(5, 'B'));
        public static readonly FastIndex B6 = new FastIndex(new Index(6, 'B'));
        public static readonly FastIndex B9 = new FastIndex(new Index(9, 'B'));
        public static readonly FastIndex B10 = new FastIndex(new Index(10, 'B'));
        public static readonly FastIndex C1 = new FastIndex(new Index(1, 'C'));
        public static readonly FastIndex C9 = new FastIndex(new Index(9, 'C'));
        public static readonly FastIndex D1 = new FastIndex(new Index(1, 'D'));
        public static readonly FastIndex E5 = new FastIndex(new Index(5, 'E'));
        public static readonly FastIndex D10 = new FastIndex(new Index(10, 'D'));
        public static readonly FastIndex E4 = new FastIndex(new Index(4, 'E'));
        public static readonly FastIndex F10 = new FastIndex(new Index(10, 'F'));
        public static readonly FastIndex G1 = new FastIndex(new Index(1, 'G'));
        public static readonly FastIndex G9 = new FastIndex(new Index(9, 'G'));
        public static readonly FastIndex H1 = new FastIndex(new Index(1, 'H'));
        public static readonly FastIndex H2 = new FastIndex(new Index(2, 'H'));
        public static readonly FastIndex H5 = new FastIndex(new Index(5, 'H'));
        public static readonly FastIndex H6 = new FastIndex(new Index(6, 'H'));
        public static readonly FastIndex H9 = new FastIndex(new Index(9, 'H'));
        public static readonly FastIndex H10 = new FastIndex(new Index(10, 'H'));
        public static readonly FastIndex I1 = new FastIndex(new Index(1, 'I'));
        public static readonly FastIndex I2 = new FastIndex(new Index(2, 'I'));
        public static readonly FastIndex I5 = new FastIndex(new Index(5, 'I'));
        public static readonly FastIndex I9 = new FastIndex(new Index(9, 'I'));
        public static readonly FastIndex I8 = new FastIndex(new Index(8, 'I'));
    }
}
