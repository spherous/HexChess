using System.Collections.Generic;
using System.Linq;

public static class PrecomputedMoveData
{
    const int NUM_HEXES = 85;

    public static readonly FastIndex[][] kingMoves = new FastIndex[85][];
    public static readonly FastIndex[][] knightMoves = new FastIndex[85][];
    public static readonly FastIndex[][] squireMoves = new FastIndex[85][];
    public static readonly FastIndex[][][] rookRays = new FastIndex[85][][];
    public static readonly FastIndex[][][] bishopRays = new FastIndex[85][][];

    public static readonly BitsBoard[] kingThreats = new BitsBoard[85];
    public static readonly BitsBoard[] squireThreats = new BitsBoard[85];
    public static readonly BitsBoard[] knightThreats = new BitsBoard[85];

    public static readonly HexNeighborDirection[] AllDirections = new HexNeighborDirection[] {
        HexNeighborDirection.Up,
        HexNeighborDirection.UpRight,
        HexNeighborDirection.DownRight,
        HexNeighborDirection.Down,
        HexNeighborDirection.DownLeft,
        HexNeighborDirection.UpLeft,
    };

    static PrecomputedMoveData()
    {
        BitsBoard threats;

        for (byte b = 0; b < NUM_HEXES; b++)
        {
            var start = FastIndex.FromByte(b);
            kingMoves[b] = GenerateAllPossibleKingMoves(start).ToArray();
            knightMoves[b] = GenerateAllPossibleKnightMoves(start).ToArray();
            squireMoves[b] = GenerateAllPossibleSquireMoves(start).ToArray();
            rookRays[b] = GenerateAllPossibleRookRays(start).ToArray();
            bishopRays[b] = GenerateAllPossibleBishopRays(start).ToArray();

            threats = new BitsBoard();
            foreach (var move in kingMoves[b])
            {
                threats[move] = true;
            }
            kingThreats[b] = threats;

            threats = new BitsBoard();
            foreach (var move in squireMoves[b])
            {
                threats[move] = true;
            }
            squireThreats[b] = threats;

            threats = new BitsBoard();
            foreach (var move in knightMoves[b])
            {
                threats[move] = true;
            }
            knightThreats[b] = threats;
        }
    }

    static IEnumerable<FastIndex> GenerateAllPossibleKingMoves(FastIndex start)
    {
        foreach (var direction in AllDirections)
        {
            if (start.TryGetNeighbor(direction, out var neighbor))
                yield return neighbor;
        }
    }

    static IEnumerable<FastIndex> GenerateAllPossibleKnightMoves(FastIndex start)
    {
        var possibleMoves = new FastIndex[]
        {
            start[HexNeighborDirection.Up][HexNeighborDirection.Up][HexNeighborDirection.UpLeft],
            start[HexNeighborDirection.Up][HexNeighborDirection.Up][HexNeighborDirection.UpRight],
            start[HexNeighborDirection.UpRight][HexNeighborDirection.UpRight][HexNeighborDirection.Up],
            start[HexNeighborDirection.UpRight][HexNeighborDirection.DownRight][HexNeighborDirection.UpRight],
            start[HexNeighborDirection.DownRight][HexNeighborDirection.UpRight][HexNeighborDirection.DownRight],
            start[HexNeighborDirection.DownRight][HexNeighborDirection.DownRight][HexNeighborDirection.Down],
            start[HexNeighborDirection.Down][HexNeighborDirection.Down][HexNeighborDirection.DownRight],
            start[HexNeighborDirection.Down][HexNeighborDirection.Down][HexNeighborDirection.DownLeft],
            start[HexNeighborDirection.DownLeft][HexNeighborDirection.DownLeft][HexNeighborDirection.Down],
            start[HexNeighborDirection.DownLeft][HexNeighborDirection.UpLeft][HexNeighborDirection.DownLeft],
            start[HexNeighborDirection.UpLeft][HexNeighborDirection.DownLeft][HexNeighborDirection.UpLeft],
            start[HexNeighborDirection.UpLeft][HexNeighborDirection.UpLeft][HexNeighborDirection.Up],
        };

        foreach (var index in possibleMoves)
        {
            if (index.IsInBounds)
                yield return index;
        }
    }

    public static readonly HexNeighborDirection[,] SquireOffsets = new HexNeighborDirection[6,2] {
        { HexNeighborDirection.Up, HexNeighborDirection.UpLeft },
        { HexNeighborDirection.UpRight, HexNeighborDirection.Up },
        { HexNeighborDirection.DownRight, HexNeighborDirection.UpRight },
        { HexNeighborDirection.Down, HexNeighborDirection.DownRight },
        { HexNeighborDirection.DownLeft, HexNeighborDirection.Down },
        { HexNeighborDirection.UpLeft, HexNeighborDirection.DownLeft },
    };

    static IEnumerable<FastIndex> GenerateAllPossibleSquireMoves(FastIndex start)
    {
        FastIndex InBounds(FastIndex idx1, FastIndex idx2)
        {
            return idx1.IsInBounds ? idx1 : idx2;
        }

        var possibleMoves = new FastIndex[]
        {
            start[HexNeighborDirection.Up][HexNeighborDirection.UpLeft],
            start[HexNeighborDirection.UpRight][HexNeighborDirection.Up],
            InBounds(start[HexNeighborDirection.DownRight][HexNeighborDirection.UpRight],
                     start[HexNeighborDirection.UpRight][HexNeighborDirection.DownRight]),
            start[HexNeighborDirection.Down][HexNeighborDirection.DownRight],
            start[HexNeighborDirection.DownLeft][HexNeighborDirection.Down],
            InBounds(start[HexNeighborDirection.UpLeft][HexNeighborDirection.DownLeft],
                     start[HexNeighborDirection.DownLeft][HexNeighborDirection.UpLeft]),
        };

        foreach (var index in possibleMoves)
        {
            if (index.IsInBounds)
                yield return index;
        }
    }

    static IEnumerable<FastIndex> GenerateRay(FastIndex start, HexNeighborDirection direction)
    {
        FastIndex target = start;
        for (int j = 0; j < 20; j++)
        {
            target = target[direction];
            if (!target.IsInBounds)
                break;

            yield return target;
        }
    }

    static IEnumerable<FastIndex[]> GenerateAllPossibleBishopRays(FastIndex start)
    {
        yield return GenerateRay(start, HexNeighborDirection.UpRight).ToArray();
        yield return GenerateRay(start, HexNeighborDirection.DownRight).ToArray();
        yield return GenerateRay(start, HexNeighborDirection.DownLeft).ToArray();
        yield return GenerateRay(start, HexNeighborDirection.UpLeft).ToArray();
    }

    static IEnumerable<FastIndex[]> GenerateAllPossibleRookRays(FastIndex start)
    {
        yield return GenerateRay(start, HexNeighborDirection.Up).ToArray();

        { // Right
            var targets = new List<FastIndex>();
            var target = start;
            for (int i = 0; i < 20; i++)
            {
                var zigUp = target[HexNeighborDirection.UpRight][HexNeighborDirection.DownRight];
                var zigDown = target[HexNeighborDirection.DownRight][HexNeighborDirection.UpRight];

                if (zigUp.IsInBounds)
                    target = zigUp;
                else if (zigDown.IsInBounds)
                    target = zigDown;
                else
                    break;

                targets.Add(target);
            }
            yield return targets.ToArray();
        }

        yield return GenerateRay(start, HexNeighborDirection.Down).ToArray();

        { // Left
            var target = start;
            var targets = new List<FastIndex>();
            for (int i = 0; i < 20; i++)
            {
                var zigUp = target[HexNeighborDirection.UpLeft][HexNeighborDirection.DownLeft];
                var zigDown = target[HexNeighborDirection.DownLeft][HexNeighborDirection.UpLeft];

                if (zigUp.IsInBounds)
                    target = zigUp;
                else if (zigDown.IsInBounds)
                    target = zigDown;
                else
                    break;

                targets.Add(target);
            }
            yield return targets.ToArray();
        }
    }
}
