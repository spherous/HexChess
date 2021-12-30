using System;

public struct BitsBoard
{
    const ulong ValidHighBits = 0b_1_1111111111_1111111111;

    ulong highBits;
    ulong lowBits;

    internal BitsBoard(ulong low, ulong high)
    {
        lowBits = low;
        highBits = high;
    }

    public int Count => BitCount_sw(highBits) + BitCount_sw(lowBits);

    public bool this[byte index]
    {
        get {
            if (index > 63)
            {
                return (highBits & ((ulong)1 << (index - 64))) != 0;
            }
            else
            {
                return (lowBits & ((ulong)1 << index)) != 0;
            }
        }
        set {
            if (index > 63)
            {
                if (value)
                {
                    highBits |= ((ulong)1 << index - 64);
                }
                else
                {
                    highBits &= ~((ulong)1 << index - 64);
                }
            }
            else
            {
                if (value)
                {
                    lowBits |= ((ulong)1 << index);
                }
                else
                {
                    lowBits &= ~((ulong)1 << index);
                }
            }
        }
    }
    public bool this[Index index]
    {
        get => this[index.ToByte()];
        set => this[index.ToByte()] = value;
    }
    public bool this[FastIndex index]
    {
        get => this[index.ToByte()];
        set => this[index.ToByte()] = value;
    }

    public static BitsBoard Pack(BitsBoard board)
    {
        const ulong B10 = 1 << (82 - 64);
        const ulong D10 = 1 << (84 - 64);
        const ulong F10 = 1 << (86 - 64);
        const ulong H10 = 1 << (88 - 64);

        const ulong B10packed = 1 << (81 - 64);
        const ulong D10packed = 1 << (82 - 64);
        const ulong F10packed = 1 << (83 - 64);
        const ulong H10packed = 1 << (84 - 64);

        const ulong noHighMask = ~(B10 | D10 | F10 | H10 | B10packed | D10packed | F10packed | H10packed);

        ulong onlyB10 = (board.highBits & B10) >> 1;
        ulong onlyD10 = (board.highBits & D10) >> 2;
        ulong onlyF10 = (board.highBits & F10) >> 3;
        ulong onlyH10 = (board.highBits & H10) >> 4;

        ulong newHigh = (board.highBits & noHighMask) | onlyB10 | onlyD10 | onlyF10 | onlyH10;

        return new BitsBoard(board.lowBits, newHigh & ValidHighBits);
    }

    public static BitsBoard Unpack(BitsBoard board)
    {
        const ulong B10 = 1 << (81 - 64);
        const ulong D10 = 1 << (82 - 64);
        const ulong F10 = 1 << (83 - 64);
        const ulong H10 = 1 << (84 - 64);
        const ulong noHighMask = ~(B10 | D10 | F10 | H10);

        ulong onlyB10 = (board.highBits & B10) << 1;
        ulong onlyD10 = (board.highBits & D10) << 2;
        ulong onlyF10 = (board.highBits & F10) << 3;
        ulong onlyH10 = (board.highBits & H10) << 4;

        ulong newHigh = (board.highBits & noHighMask) | onlyB10 | onlyD10 | onlyF10 | onlyH10;

        return new BitsBoard(board.lowBits, newHigh);
    }

    /// <summary>
    /// Count the number of bits set to 1 in a ulong
    /// </summary>
    public static byte BitCount_sw(ulong value)
    {
        // From .NET framework, MIT licensed
        const ulong c1 = 0x_55555555_55555555ul;
        const ulong c2 = 0x_33333333_33333333ul;
        const ulong c3 = 0x_0F0F0F0F_0F0F0F0Ful;
        const ulong c4 = 0x_01010101_01010101ul;

        value -= (value >> 1) & c1;
        value = (value & c2) + ((value >> 2) & c2);
        value = (((value + (value >> 4)) & c3) * c4) >> 56;

        return (byte)value;
    }

    public static BitsBoard operator &(BitsBoard a, BitsBoard b)
    {
        return new BitsBoard(a.lowBits & b.lowBits, a.highBits & b.highBits);
    }
    public static BitsBoard operator ^(BitsBoard a, BitsBoard b)
    {
        return new BitsBoard(a.lowBits ^ b.lowBits, a.highBits ^ b.highBits);
    }
    public static BitsBoard operator |(BitsBoard a, BitsBoard b)
    {
        return new BitsBoard(a.lowBits | b.lowBits, a.highBits | b.highBits);
    }
    public static BitsBoard operator ~(BitsBoard a)
    {
        return new BitsBoard(~a.lowBits, ~a.highBits);
    }
    public static BitsBoard operator <<(BitsBoard self, int shift)
    {
        if (shift > 63)
        {
            var high = (self.lowBits << (shift - 64));
            return new BitsBoard(0, high);
        }
        else
        {
            var high = (self.highBits << shift) | (self.lowBits >> (64 - shift));
            var low = self.lowBits << shift;
            return new BitsBoard(low, high);
        }
    }
    public static BitsBoard operator >>(BitsBoard self, int shift)
    {
        if (shift > 63)
        {
            var low = (self.highBits >> (shift - 64));
            return new BitsBoard(low, 0);
        }
        else
        {
            var low = (self.lowBits >> shift) | (self.highBits << (64 - shift));
            var high = self.highBits >> shift;
            return new BitsBoard(low, high);
        }
    }

    public BitsBoard Shift(HexNeighborDirection direction)
    {
        var unpacked = Unpack(this);
        switch (direction)
        {
            case HexNeighborDirection.Up:
                return Pack(unpacked << 9);
            case HexNeighborDirection.Down:
                return Pack(unpacked >> 9);

            case HexNeighborDirection.UpRight:
                unpacked &= Masks.HasUpRight;
                unpacked = ((unpacked & Masks.Short) << 10) | ((unpacked & Masks.Tall) << 1);
                return Pack(unpacked);
            case HexNeighborDirection.DownRight:
                unpacked &= Masks.HasDownRight;
                unpacked = ((unpacked & Masks.Short) << 1) | ((unpacked & Masks.Tall) >> 8);
                return Pack(unpacked);
            case HexNeighborDirection.DownLeft:
                unpacked &= Masks.HasDownLeft;
                unpacked = ((unpacked & Masks.Short) >> 1) | ((unpacked & Masks.Tall) >> 10);
                return Pack(unpacked);
            case HexNeighborDirection.UpLeft:
                unpacked &= Masks.HasUpLeft;
                unpacked = ((unpacked & Masks.Short) << 8) | ((unpacked & Masks.Tall) >> 1);
                return Pack(unpacked);

            default:
                throw new ArgumentException($"Invalid HexNeighborDirection {(int)direction}");
        }
    }

    public static class Masks
    {
        public static readonly BitsBoard A1 = new BitsBoard(1, 0);
        public static readonly BitsBoard A2 = A1 << 9;
        public static readonly BitsBoard A3 = A1 << 18;
        public static readonly BitsBoard A4 = A1 << 27;
        public static readonly BitsBoard A5 = A1 << 36;
        public static readonly BitsBoard A6 = A1 << 45;
        public static readonly BitsBoard A7 = A1 << 54;
        public static readonly BitsBoard A8 = A1 << 63;
        public static readonly BitsBoard A9 = A1 << 72;
        public static readonly BitsBoard B1 = A1 << 1;
        public static readonly BitsBoard B2 = B1 << 9;
        public static readonly BitsBoard B3 = B1 << 18;
        public static readonly BitsBoard B4 = B1 << 27;
        public static readonly BitsBoard B5 = B1 << 36;
        public static readonly BitsBoard B6 = B1 << 45;
        public static readonly BitsBoard B7 = B1 << 54;
        public static readonly BitsBoard B8 = B1 << 63;
        public static readonly BitsBoard B9 = B1 << 72;
        public static readonly BitsBoard B10 = B1 << 81;
        public static readonly BitsBoard C1 = B1 << 1;
        public static readonly BitsBoard C2 = C1 << 9;
        public static readonly BitsBoard C3 = C1 << 18;
        public static readonly BitsBoard C4 = C1 << 27;
        public static readonly BitsBoard C5 = C1 << 36;
        public static readonly BitsBoard C6 = C1 << 45;
        public static readonly BitsBoard C7 = C1 << 54;
        public static readonly BitsBoard C8 = C1 << 63;
        public static readonly BitsBoard C9 = C1 << 72;
        public static readonly BitsBoard D1 = C1 << 1;
        public static readonly BitsBoard D2 = D1 << 9;
        public static readonly BitsBoard D3 = D1 << 18;
        public static readonly BitsBoard D4 = D1 << 27;
        public static readonly BitsBoard D5 = D1 << 36;
        public static readonly BitsBoard D6 = D1 << 45;
        public static readonly BitsBoard D7 = D1 << 54;
        public static readonly BitsBoard D8 = D1 << 63;
        public static readonly BitsBoard D9 = D1 << 72;
        public static readonly BitsBoard D10 = D1 << 81;
        public static readonly BitsBoard E1 = D1 << 1;
        public static readonly BitsBoard E2 = E1 << 9;
        public static readonly BitsBoard E3 = E1 << 18;
        public static readonly BitsBoard E4 = E1 << 27;
        public static readonly BitsBoard E5 = E1 << 36;
        public static readonly BitsBoard E6 = E1 << 45;
        public static readonly BitsBoard E7 = E1 << 54;
        public static readonly BitsBoard E8 = E1 << 63;
        public static readonly BitsBoard E9 = E1 << 72;
        public static readonly BitsBoard F1 = E1 << 1;
        public static readonly BitsBoard F2 = F1 << 9;
        public static readonly BitsBoard F3 = F1 << 18;
        public static readonly BitsBoard F4 = F1 << 27;
        public static readonly BitsBoard F5 = F1 << 36;
        public static readonly BitsBoard F6 = F1 << 45;
        public static readonly BitsBoard F7 = F1 << 54;
        public static readonly BitsBoard F8 = F1 << 63;
        public static readonly BitsBoard F9 = F1 << 72;
        public static readonly BitsBoard F10 = F1 << 81;
        public static readonly BitsBoard G1 = F1 << 1;
        public static readonly BitsBoard G2 = G1 << 9;
        public static readonly BitsBoard G3 = G1 << 18;
        public static readonly BitsBoard G4 = G1 << 27;
        public static readonly BitsBoard G5 = G1 << 36;
        public static readonly BitsBoard G6 = G1 << 45;
        public static readonly BitsBoard G7 = G1 << 54;
        public static readonly BitsBoard G8 = G1 << 63;
        public static readonly BitsBoard G9 = G1 << 72;
        public static readonly BitsBoard H1 = G1 << 1;
        public static readonly BitsBoard H2 = H1 << 9;
        public static readonly BitsBoard H3 = H1 << 18;
        public static readonly BitsBoard H4 = H1 << 27;
        public static readonly BitsBoard H5 = H1 << 36;
        public static readonly BitsBoard H6 = H1 << 45;
        public static readonly BitsBoard H7 = H1 << 54;
        public static readonly BitsBoard H8 = H1 << 63;
        public static readonly BitsBoard H9 = H1 << 72;
        public static readonly BitsBoard H10 = H1 << 81;
        public static readonly BitsBoard I1 = H1 << 1;
        public static readonly BitsBoard I2 = I1 << 9;
        public static readonly BitsBoard I3 = I1 << 18;
        public static readonly BitsBoard I4 = I1 << 27;
        public static readonly BitsBoard I5 = I1 << 36;
        public static readonly BitsBoard I6 = I1 << 45;
        public static readonly BitsBoard I7 = I1 << 54;
        public static readonly BitsBoard I8 = I1 << 63;
        public static readonly BitsBoard I9 = I1 << 72;

        public static readonly BitsBoard AFile = A1 | A2 | A3 | A4 | A5 | A6 | A7 | A8 | A9;
        public static readonly BitsBoard BFile = B1 | B2 | B3 | B4 | B5 | B6 | B7 | B8 | B9 | B10;
        public static readonly BitsBoard CFile = C1 | C2 | C3 | C4 | C5 | C6 | C7 | C8 | C9;
        public static readonly BitsBoard DFile = D1 | D2 | D3 | D4 | D5 | D6 | D7 | D8 | D9 | D10;
        public static readonly BitsBoard EFile = E1 | E2 | E3 | E4 | E5 | E6 | E7 | E8 | E9;
        public static readonly BitsBoard FFile = F1 | F2 | F3 | F4 | F5 | F6 | F7 | F8 | F9 | F10;
        public static readonly BitsBoard GFile = G1 | G2 | G3 | G4 | G5 | G6 | G7 | G8 | G9;
        public static readonly BitsBoard HFile = H1 | H2 | H3 | H4 | H5 | H6 | H7 | H8 | H9 | H10;
        public static readonly BitsBoard IFile = I1 | I2 | I3 | I4 | I5 | I6 | I7 | I8 | I9;

        public static readonly BitsBoard Short = AFile | CFile | EFile | GFile | IFile;
        public static readonly BitsBoard Tall = BFile | DFile | FFile | HFile;

        public static readonly BitsBoard All = Short | Tall;
        public static readonly BitsBoard HasUpRight = All ^ IFile ^ B10 ^ D10 ^ F10 ^ H10;
        public static readonly BitsBoard HasDownRight= All ^ IFile ^ B1 ^ D1 ^ F1 ^ H1;
        public static readonly BitsBoard HasDownLeft= All ^ AFile ^ B1 ^ D1 ^ F1 ^ H1;
        public static readonly BitsBoard HasUpLeft= All ^ AFile ^ B10 ^ D10 ^ F10 ^ H10;
    }
}
