using System;

public readonly struct FastIndex : IEquatable<FastIndex>
{
    readonly static FastIndex[] NeighborMap;
    readonly static FastIndex[] MirrorMap;

    static FastIndex()
    {
        var directions = EnumArray<HexNeighborDirection>.Values;
        NeighborMap = new FastIndex[85 * directions.Length];
        MirrorMap = new FastIndex[85];
        for (byte i = 0; i < 85; i++)
        {
            var slowIndex = Index.CalculateFromByte(i);
            int offsetStart = i * directions.Length;

            for (int j = 0; j < directions.Length; j++)
            {
                HexNeighborDirection direction = directions[j];
                FastIndex neighbor = FastIndex.Invalid;
                if (slowIndex.TryGetNeighbor(direction, out Index slowNeighbor))
                    neighbor = new FastIndex(slowNeighbor);
                NeighborMap[offsetStart + j] = neighbor;
            }

            MirrorMap[i] = CalculateMirror(FromByte(i));
        }
    }

    public static readonly FastIndex Invalid = new FastIndex(byte.MaxValue);

    public static FastIndex FromByte(byte b)
    {
        var index = new FastIndex(b);
        return index.IsInBounds ? index : Invalid;
    }

    public readonly byte HexId;
    public bool IsInBounds => HexId < 85;

    internal FastIndex(byte b)
    {
        this.HexId = b;
    }

    public FastIndex(Index fromIndex)
    {
        this.HexId = fromIndex.ToByte();
    }

    public FastIndex(int rank, char file)
    {
        this.HexId = new Index(rank, file).ToByte();
    }

    public FastIndex this[HexNeighborDirection dir]
    {
        get
        {
            if (TryGetNeighbor(dir, out FastIndex neighbor))
                return neighbor;
            return Invalid;
        }
    }

    public bool TryGetNeighbor(HexNeighborDirection direction, out FastIndex index)
    {
        if (IsInBounds)
        {
            index = NeighborMap[HexId * 6 + (int)direction];
            return index.IsInBounds;
        }
        else
        {
            index = Invalid;
            return false;
        }

    }

    public FastIndex? GetNeighborAt(HexNeighborDirection direction)
    {
        if (TryGetNeighbor(direction, out FastIndex index))
            return index;
        return null;
    }

    public byte ToByte()
    {
        return HexId;
    }

    public override string ToString()
    {
        if (!IsInBounds)
            return $"Invalid ({HexId})";

        var slow = Index.FromByte(HexId);
        return $"{slow} ({HexId})";
    }

    public bool Equals(FastIndex other)
    {
        return HexId == other.HexId;
    }

    public override bool Equals(Object obj)
    {
        if (obj == null || !(obj is FastIndex index))
            return false;
        else
            return HexId == index.HexId;
    }

    public static explicit operator FastIndex(Index index) => new FastIndex(index);
    public static explicit operator Index(FastIndex index) => Index.FromByte(index.HexId);

    public static bool operator ==(FastIndex a, FastIndex b) => a.HexId == b.HexId;
    public static bool operator !=(FastIndex a, FastIndex b) => a.HexId != b.HexId;
    public override int GetHashCode()
    {
        return HexId.GetHashCode();
    }

    public FastIndex Mirror()
    {
        return MirrorMap[HexId];
    }

    public static FastIndex CalculateMirror(FastIndex from)
    {
        Index index = Index.FromByte(from.HexId);

        int rank = index.GetNumber();
        char file = index.GetLetter();
        bool isTall = file == 'B' || file == 'D' || file == 'F' || file == 'H';

        if (isTall)
            rank = 11 - rank;
        else
            rank = 10 - rank;

        return new FastIndex(rank, file);
    }

}
