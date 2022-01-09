using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public class TeriteAI : IHexAI
{
    public DiagnosticInfo diagnostics = new DiagnosticInfo();

    readonly int maxSearchDepth;
    readonly bool quiescenceSearchEnabled = true;
    public bool iterativeDeepeningEnabled = true;
    public bool previousOrderingEnabled = true;
    public bool pawnValueMapEnabled = false;

    readonly Dictionary<FastMove, int> previousScores = new Dictionary<FastMove, int>();
    readonly List<FastMove>[] moveCache;
    volatile bool cancellationRequested;

    public readonly EvaluationData evaluationData = new EvaluationData();

    FastMove bestMove;
    FastMove bestMoveThisIteration;
    int currentDepth;

    public TeriteAI(int maxSearchDepth = 4)
    {
        this.maxSearchDepth = maxSearchDepth;
        moveCache = new List<FastMove>[this.maxSearchDepth + 1 /* root */];

        for (int i = 0; i < moveCache.Length; i++)
            moveCache[i] = new List<FastMove>(100);
    }

    Task<HexAIMove> IHexAI.GetMove(Game game)
    {
        var root = new FastBoardNode(game);
        return Task.Run(() =>
        {
            return GetMove(root).ToHexMove();
        });
    }

    void IHexAI.CancelMove()
    {
        cancellationRequested = true;
    }

    IEnumerable<string> IHexAI.GetDiagnosticInfo()
    {
        yield return $"Cancellation requested: {cancellationRequested}";
        yield return $"bestMove: {bestMove}";
        yield return $"bestMoveThisIteration: {bestMoveThisIteration}";
        yield return $"currentDepth: {currentDepth}";
    }

    public FastMove GetMove(FastBoardNode root)
    {
        diagnostics = new DiagnosticInfo();
        using (diagnostics.getMove.Measure())
        {
            previousScores.Clear();

            int color = (root.currentMove == Team.White) ? 1 : -1;
            int minSearchDepth = iterativeDeepeningEnabled ? 1 : maxSearchDepth;
            bestMove = FastMove.Invalid;
            currentDepth = minSearchDepth;
            cancellationRequested = false;

            for (; currentDepth <= maxSearchDepth; currentDepth++)
            {
                bestMoveThisIteration = FastMove.Invalid;
                int alpha = -CheckmateValue * 2; // Best move for current player
                int beta = CheckmateValue * 2; // Best move our opponent will let us have
                int moveValue = Search(root, currentDepth, 0, alpha, beta, color);

                bestMove = bestMoveThisIteration;

                if (moveValue >= (CheckmateValue - currentDepth))
                {
                    return bestMove;
                }
            }

            return bestMove;
        }
    }

    #region Searching

    int Search(FastBoardNode node, int searchDepth, int plyFromRoot, int alpha, int beta, int color)
    {
        if (searchDepth == 0)
        {
            using (diagnostics.quiescence.Measure())
                return QuiescenceSearch(node, plyFromRoot, alpha, beta, color);
        }

        List<FastMove> moves;
        using (diagnostics.moveGen.Measure())
        {
            moves = moveCache[searchDepth];
            moves.Clear();
            node.AddAllPossibleMoves(moves, node.currentMove);
        }

        evaluationData.Prepare(node);

        using (diagnostics.moveSort.Measure())
        {
            OrderMoves(node, moves, plyFromRoot);
        }

        bool isTerminal = true;
        int value = int.MinValue;

        foreach (var move in moves)
        {
            if (cancellationRequested)
                return 0;

            using (diagnostics.apply.Measure())
                node.DoMove(move);

            bool isKingVulnerable;
            using (diagnostics.moveValidate.Measure())
                isKingVulnerable = node.IsChecking(node.currentMove);

            if (isKingVulnerable)
            {
                diagnostics.invalidMoves++;
                using (diagnostics.apply.Measure())
                    node.UndoMove(move);
                continue;
            }

            isTerminal = false;
            int currentValue = -Search(node, searchDepth - 1, plyFromRoot + 1, -beta, -alpha, -color);

            if (previousOrderingEnabled && plyFromRoot == 0)
                previousScores[move] = currentValue;

            using (diagnostics.apply.Measure())
                node.UndoMove(move);

            if (currentValue > value)
            {
                if (plyFromRoot == 0)
                {
                    bestMoveThisIteration = move;
                }
                value = currentValue;
            }
            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
            {
                diagnostics.searchCutoff++;
                break;
            }
        }

        if (isTerminal)
        {
            value = color * EvaluateTerminalBoard(node, plyFromRoot);
        }

        return value;
    }

    int QuiescenceSearch(FastBoardNode node, int plyFromRoot, int alpha, int beta, int color)
    {
        int eval;

        evaluationData.Prepare(node);

        using (diagnostics.quiescenceEval.Measure())
            eval = color * EvaluateBoard(node, plyFromRoot);

        if (!quiescenceSearchEnabled)
            return eval;

        if (eval >= beta)
        {
            diagnostics.quiescenceCutoff++;
            return beta;
        }

        if (eval > alpha)
            alpha = eval;

        List<FastMove> moves;
        using (diagnostics.quiescenceMoveGen.Measure())
        {
            moves = new List<FastMove>(10);
            node.AddAllPossibleMoves(moves, node.currentMove, generateQuiet: false);
        }

        using (diagnostics.quiescenceMoveSort.Measure())
            OrderMoves(node, moves, -1);

        bool maybeTerminal = true;
        int value = int.MinValue;

        foreach (var move in moves)
        {
            if (cancellationRequested)
                return 0;

            using (diagnostics.quiescenceApply.Measure())
                node.DoMove(move);

            bool isKingVulnerable;
            using (diagnostics.quiescenceMoveValidate.Measure())
                isKingVulnerable = node.IsChecking(node.currentMove);
            if (isKingVulnerable)
            {
                diagnostics.invalidMoves++;
                using (diagnostics.quiescenceApply.Measure())
                    node.UndoMove(move);
                continue;
            }

            maybeTerminal = false;
            int currentValue = -QuiescenceSearch(node, plyFromRoot + 1, -beta, -alpha, -color);

            using (diagnostics.quiescenceApply.Measure())
                node.UndoMove(move);

            if (currentValue > value)
            {
                value = currentValue;
            }
            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
            {
                diagnostics.quiescenceCutoff++;
                break;
            }
        }

        // No non-quiet moves were found from this position
        if (maybeTerminal)
            return eval;

        return value;
    }

    #endregion

    #region Move Ordering

    public readonly struct ScoredMove : IComparable<ScoredMove>
    {
        public readonly FastMove move;
        public readonly int score;
        public ScoredMove(FastMove move, int score)
        {
            this.move = move;
            this.score = score;
        }

        public int CompareTo(ScoredMove other)
        {
            return other.score - score; // descending
        }
    }

    readonly List<ScoredMove> sortCache = new List<ScoredMove>(100);
    private void OrderMoves(FastBoardNode node, List<FastMove> moves, int plyFromRoot)
    {
        var scoredMoves = sortCache;
        scoredMoves.Clear();

        if (previousOrderingEnabled && plyFromRoot == 0 && previousScores.Count > 0)
        {
            foreach (var move in moves)
            {
                previousScores.TryGetValue(move, out int score);
                scoredMoves.Add(new ScoredMove(move, score));
            }
        }
        else
        {
            foreach (var move in moves)
            {
                scoredMoves.Add(new ScoredMove(move, MoveValuer(node, move)));
            }
        }

        scoredMoves.Sort();
        for (int i = 0; i < moves.Count; i++)
        {
            moves[i] = scoredMoves[i].move;
        }
    }

    private int MoveValuer(FastBoardNode node, FastMove move)
    {
        int value = 1000; // Start high to always exceed invalid move scores of 0

        var mover = node[move.start];
        int attackerValue = GetPieceValue(mover.piece);

        EvaluationData.BoardCollection us;
        EvaluationData.BoardCollection them;
        if (node.currentMove == Team.White)
        {
            us = evaluationData.White;
            them = evaluationData.Black;
        }
        else
        {
            us = evaluationData.Black;
            them = evaluationData.White;
        }

        // Devalue moving into threatened hexes
        if (them.Threats[move.target])
        {
            value -= attackerValue / 2;
        }

        // Value moving threatened pieces
        if (them.Threats[move.start])
        {
            value += attackerValue / 2;
        }

        if (move.moveType == MoveType.Move)
        {
            // TODO: what else?
        }
        else if (move.moveType == MoveType.Attack)
        {
            bool isFree = !them.Threats[move.target];
            var victim = node[move.target];
            if (isFree)
            {
                value += GetPieceValue(victim.piece) / 2;
            }
            else
            {
                int attackValue = GetPieceValue(victim.piece) - attackerValue;
                value += attackValue / 10;
            }
        }
        else if (move.moveType == MoveType.EnPassant)
        {
            value += 5;
        }

        return value;
    }

    #endregion

    #region Evaluation

    const int CheckBonusValue = 10;
    const int CheckmateValue = 10000;
    const int DrawValue = 0;

    static readonly int[] TeamMults = new[] { 0, 1, -1 };

    public int EvaluateTerminalBoard(FastBoardNode node, int plyFromRoot)
    {
        diagnostics.terminalBoardEvaluations++;
        bool whiteIsChecking = node.currentMove != Team.White && node.IsChecking(Team.White);
        if (whiteIsChecking)
        {
            return CheckmateValue - plyFromRoot;
        }

        bool blackIsChecking = node.currentMove != Team.Black && node.IsChecking(Team.Black);
        if (blackIsChecking)
        {
            return -CheckmateValue + plyFromRoot;
        }

        // Either stalemate, or 50 move rule draw
        return DrawValue;
    }

    public int EvaluateBoard(FastBoardNode node, int plyFromRoot)
    {
        if (node.plySincePawnMovedOrPieceTaken >= 100)
            return 0; // automatic draw due to 50 move rule.

        int boardValue = 0;
        bool whiteIsChecking = node.currentMove != Team.White && node.IsChecking(Team.White);
        if (whiteIsChecking)
        {
            boardValue += CheckBonusValue;
        }

        bool blackIsChecking = node.currentMove != Team.Black && node.IsChecking(Team.Black);
        if (blackIsChecking)
        {
            boardValue -= CheckBonusValue;
        }

        if (!node.HasAnyValidMoves(node.currentMove))
        {
            if (whiteIsChecking)
                return CheckmateValue - plyFromRoot;
            else if (blackIsChecking)
                return -CheckmateValue + plyFromRoot;
            else
                return DrawValue;
        }

        /*
        for (byte i = 0; i < node.positions.Length; i++)
        {
            var piece = node.positions[i];
            if (piece.team == Team.None)
                continue;

            var valuedPosition = FastIndex.FromByte(i);
            if (piece.team == Team.Black)
                valuedPosition = valuedPosition.Mirror();

            int pieceValue = GetPieceValue(piece.piece);

            if (pawnValueMapEnabled && piece.piece == FastPiece.Pawn)
            {
                pieceValue += pawnValueMap[valuedPosition.HexId];
            }

            boardValue += TeamMults[(byte)piece.team] * pieceValue;
        }
        */

        using (diagnostics.evalThreats.Measure())
        {
            boardValue += evaluationData.White.Threats.Count - evaluationData.Black.Threats.Count;
            boardValue += (evaluationData.White.PawnThreats.Count * 2) - (evaluationData.Black.PawnThreats.Count * 2);
            boardValue += evaluationData.White.MaterialValue - evaluationData.Black.MaterialValue;

            var whiteAttacks = evaluationData.Black.Pieces & evaluationData.White.Threats;
            var blackAttacks = evaluationData.White.Pieces & evaluationData.Black.Threats;
            boardValue += whiteAttacks.Count - blackAttacks.Count;

            var whiteDefended = evaluationData.White.Pieces & evaluationData.White.Threats;
            var blackDefended = evaluationData.Black.Pieces & evaluationData.Black.Threats;
            boardValue += whiteDefended.Count - blackDefended.Count;

            var whiteHangingPieces = blackAttacks & ~evaluationData.White.Threats;
            var blackHangingPieces = whiteAttacks & ~evaluationData.Black.Threats;
            boardValue += (blackHangingPieces.Count - whiteHangingPieces.Count) * 100;
        }

        return boardValue;
    }

    static int GetPieceValue(FastPiece piece)
    {
        switch (piece)
        {
            case FastPiece.King:
                return 10000;

            case FastPiece.Queen:
                return 1000;

            case FastPiece.Rook:
                return 525;

            case FastPiece.Knight:
                return 350;

            case FastPiece.Bishop:
                return 350;

            case FastPiece.Squire:
                return 300;

            case FastPiece.Pawn:
            default:
                return 100;
        }
    }

    #endregion

    #region Static precomputation

    static readonly int[] pawnValueMap;
    static readonly int[][] pawnValueMapSource = new int[][]
    {
        //            A    B    C    D    E    F    G    H    I
        new int[] {      100,      100,      100,      100 },      // 10
        new int[] { 100,  50, 100,  50, 100,  50, 100,  50, 100 }, // 9
        new int[] {  50,  10,  50,  10,  50,  10,  50,  10,  10 }, // 8
        new int[] {  10,  10,  10,  10,  10,  10,  10,  10,  10 }, // 7
        new int[] {  10,  10,  10,  10,  10,  10,  10,  10,  10 }, // 6
        new int[] {  10,  10,  10,  10,  10,  10,  10,  10,  10 }, // 5
        new int[] {  10,  10,  10,  10,  10,  10,  10,  10,  10 }, // 4
        new int[] {  10,  10,  10,  10,  10,  10,  10,  10,  10 }, // 3
        new int[] {  10,   0,  10,   0,  10,   0,  10,   0,  10 }, // 2
        new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0 }, // 1
        //            A    B    C    D    E    F    G    H    I
    };
    static TeriteAI()
    {
        pawnValueMap = pawnValueMapSource.Reverse().SelectMany(n => n).ToArray();
    }

    #endregion

    public class DiagnosticInfo
    {
        public readonly Section moveGen = new Section();
        public readonly Section moveSort = new Section();
        public readonly Section moveValidate = new Section();
        public readonly Section quiescenceEval = new Section();
        public readonly Section quiescence = new Section();
        public readonly Section quiescenceMoveSort = new Section();
        public readonly Section quiescenceMoveGen = new Section();
        public readonly Section quiescenceMoveValidate = new Section();
        public readonly Section quiescenceApply = new Section();
        public readonly Section evalThreats = new Section();
        public readonly Section apply = new Section();
        public readonly Section getMove = new Section();

        public int terminalBoardEvaluations = 0;
        public int invalidMoves = 0;
        public int searchCutoff = 0;
        public int quiescenceCutoff = 0;

        public class Section
        {
            public int measurements;
            public readonly Stopwatch watch = new Stopwatch();

            public Measurer Measure()
            {
                this.measurements++;
                return new Measurer(watch);
            }

            public override string ToString()
            {
                var perSecond = Math.Floor(measurements / watch.Elapsed.TotalSeconds);

                double each;
                if (measurements > 0)
                    each = watch.Elapsed.Ticks / (double)measurements;
                else
                    each = 0;

                return $"{perSecond:N0} per second. {measurements:N0} calls over {FormatNanos(watch.ElapsedTicks)} ({FormatNanos(each)})";
            }

            static string FormatNanos(double ticks)
            {
                const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

                if ((ticks / TimeSpan.TicksPerSecond) >= 1)
                {
                    return $"{ticks / TimeSpan.TicksPerSecond:0.0} sec";
                }
                if ((ticks / TimeSpan.TicksPerMillisecond) >= 1)
                {
                    return $"{ticks / TimeSpan.TicksPerMillisecond:0.0} ms";
                }
                if ((ticks / TimeSpan.TicksPerMillisecond) >= 1)
                {
                    return $"{ticks / TimeSpan.TicksPerMillisecond:0.0} ms";
                }
                if ((ticks / TicksPerMicrosecond) >= 1)
                {
                    return $"{ticks / TicksPerMicrosecond:0.0} μs";
                }

                return $"{ticks * 100:0.0} ns";
            }
        }

        public struct Measurer : IDisposable
        {
            readonly Stopwatch stopwatch;
            public Measurer(Stopwatch stopwatch)
            {
                stopwatch.Start();
                this.stopwatch = stopwatch;
            }
            public void Dispose()
            {
                stopwatch.Stop();
            }
        }
    }
}
