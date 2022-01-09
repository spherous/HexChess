using System;
using System.Collections.Generic;

[Serializable]
public struct Index
{
    public const int cols = 5;
    public const int maxCol = cols - 1;
    public const int rows = 19;
    public const int maxRow = rows - 1;

    static readonly Index[] IndexByteLookup = new Index[85];
    static readonly byte[,] ByteColRowLookup = new byte[cols, rows];

    static Index()
    {
        for (byte b = 0; b < 85; b++)
        {
            var index = CalculateFromByte(b);
            IndexByteLookup[b] = index;
            ByteColRowLookup[index.col, index.row] = b;
        }
    }

    public int row;
    public int col;

    public static readonly Index invalid = new Index(-1, -1);

    public Index(int rank, char file)
    {
        if(rank < 1 || rank > 10)
            throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be between 1-10 inclusive");
        if(file < 'A' || file > 'I')
            throw new ArgumentOutOfRangeException(nameof(file), "File must be between A-I inclusive");

        bool tallFile = file == 'B' || file == 'D' || file == 'F' || file == 'H';

        if(rank == 10 && !tallFile)
            throw new ArgumentOutOfRangeException(nameof(file), $"Only valid rank 10 files are B, D, F, H, not {file}");

        this.col = file switch {
            'A' => 0,
            'B' => 0,
            'C' => 1,
            'D' => 1,
            'E' => 2,
            'F' => 2,
            'G' => 3,
            'H' => 3,
            'I' => 4,
            _ => '?'
        };

        var startingRow = tallFile ? 0 : 1;
        this.row = startingRow + ((rank - 1) * 2);

    }
    public Index(int row, int col)
    {
        this.row = row;
        this.col = col;
    }

    public int GetSingleVal() => row < 0 || col < 0 ? -1 : int.Parse(string.Concat($"{row:D2}", $"{col}"));

    public Index this[HexNeighborDirection dir]
    {
        get
        {
            if(TryGetNeighbor(dir, out Index neighbor))
                return neighbor;
            return Index.invalid;
        }
    }

    public bool IsInBounds
    {
        get
        {
            bool cond1 = row * (row - maxRow) <= 0 && col * (col - maxCol) <= 0;
            bool cond2 = !(cols % 2 != 0 && col == cols - 1 && row % 2 == 0);
            return cond1 && cond2;
        }
    }

    public string GetKey() => $"{GetLetter()}{GetNumber()}";

    public int GetNumber() => (row / 2) + 1;

    public char GetLetter()
    {
        bool isEven = row % 2 == 0;

        return col switch {
            0 when !isEven => 'A', 0 when isEven => 'B',
            1 when !isEven => 'C', 1 when isEven => 'D',
            2 when !isEven => 'E', 2 when isEven => 'F',
            3 when !isEven => 'G', 3 when isEven => 'H',
            4 => 'I', _ => 'J'
        };
    }

    public bool TryGetNeighbor(HexNeighborDirection dir, out Index neighbor)
    {
        bool isEven = row % 2 == 0;
        (int row, int col) offsets = dir switch {
            HexNeighborDirection.Up => (2, 0),
            HexNeighborDirection.UpRight => isEven ? (1, 1) : (1, 0),
            HexNeighborDirection.DownRight => isEven ? (-1, 1) : (-1, 0),
            HexNeighborDirection.Down => (-2, 0),
            HexNeighborDirection.DownLeft => isEven ? (-1, 0) : (-1, -1),
            HexNeighborDirection.UpLeft => isEven ? (1, 0) : (1, -1),
            _ => (-100, -100)
        };

        neighbor = new Index(row + offsets.row, col + offsets.col);
        return neighbor.IsInBounds;
    }

    public Index? GetNeighborAt(HexNeighborDirection dir)
    {
        if(TryGetNeighbor(dir, out Index neighbor))
            return neighbor;

        return null;
    }

    public static IEnumerable<Index> GetAllIndices()
    {
        for(int row = 0; row <= maxRow; row++)
        {
            for(int col = 0; col <= maxCol; col++)
            {
                if(cols % 2 != 0 && col == cols - 1 && row % 2 == 0)
                    continue;

                yield return new Index(row, col);
            }
        }
    }

    public static Index CalculateFromByte(byte v)
    {
        int rank = (v / 9) + 1;
        int fileVal = (v % 9);
        char file;
        if (rank == 10)
            file = (char)('B' + (fileVal * 2));
        else
            file = (char)('A' + fileVal);

        return new Index(rank, file);
    }

    public static Index FromByte(byte v) => IndexByteLookup[v];

    public byte ToByte() => ByteColRowLookup[col, row];

    public override string ToString() => $"{row}, {col} ({GetKey()})";

    public override bool Equals(object obj) =>
        obj is Index index &&
        row == index.row &&
        col == index.col;

    public override int GetHashCode()
    {
        int hashCode = -1720622044;
        hashCode = hashCode * -1521134295 + row.GetHashCode();
        hashCode = hashCode * -1521134295 + col.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(Index a, Index b) => a.row == b.row && a.col == b.col;
    public static bool operator !=(Index a, Index b) => !(a==b);
}