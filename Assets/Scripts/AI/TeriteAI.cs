using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Extensions;

public class TeriteAI : IHexAI
{
    public readonly DiagnosticInfo diagnostics = new DiagnosticInfo();

    readonly int maxSearchDepth;
    readonly bool quiescenceSearchEnabled = true;
    public bool iterativeDeepeningEnabled = true;
    public bool previousOrderingEnabled = true;
    public bool pawnValueMapEnabled = true;

    readonly Dictionary<FastMove, int> previousScores = new Dictionary<FastMove, int>();
    readonly List<FastMove>[] moveCache;
    volatile bool cancellationRequested;
    readonly EvaluationData evaluationData = new EvaluationData();

    FastMove bestMove;
    int currentDepth;

    public TeriteAI() : this(4)
    {
    }

    public TeriteAI(int maxSearchDepth = 4)
    {
        this.maxSearchDepth = maxSearchDepth;
        moveCache = new List<FastMove>[this.maxSearchDepth + 1 /* root */];

        for (int i = 0; i < moveCache.Length; i++)
            moveCache[i] = new List<FastMove>(100);
    }
    public Task<HexAIMove> GetMove(Game game)
    {
        var root = new FastBoardNode(game);
        return Task.Run(() =>
        {
            return GetMove(root).ToHexMove();
        });
    }

    public FastMove GetMove(FastBoardNode root)
    {
        diagnostics.Reset();
        diagnostics.getMoveTimer.Start();

        int color = (root.currentMove == Team.White) ? 1 : -1;

        bestMove = FastMove.Invalid;
        int minSearchDepth = iterativeDeepeningEnabled ? 1 : maxSearchDepth;
        currentDepth = minSearchDepth;
        cancellationRequested = false;

        previousScores.Clear();

        for (; currentDepth <= maxSearchDepth; currentDepth++)
        {
            int alpha = -CheckmateValue * 2; // Best move for current player
            int beta = CheckmateValue * 2; // Best move our opponent will let us have

            (int moveValue, FastMove move) = Search(root, currentDepth, 0, alpha, beta, color);

            bestMove = move;

            if (moveValue >= (CheckmateValue - currentDepth))
            {
                diagnostics.getMoveTimer.Stop();
                return bestMove;
            }
        }

        diagnostics.getMoveTimer.Stop();
        return bestMove;
    }

    #region Searching

    (int value, FastMove move) Search(FastBoardNode node, int searchDepth, int plyFromRoot, int alpha, int beta, int color)
    {
        if (searchDepth == 0)
        {
            using (diagnostics.MeasureQuiescence())
                return (QuiescenceSearch(node, plyFromRoot, alpha, beta, color), FastMove.Invalid);
        }

        List<FastMove> moves;
        using (diagnostics.MeasureMoveGen())
        {
            moves = moveCache[searchDepth];
            moves.Clear();
            node.AddAllPossibleMoves(moves, node.currentMove);
        }

        using (diagnostics.MeasureMoveSort())
        {
            OrderMoves(node, moves, plyFromRoot);
        }

        bool isTerminal = true;
        int value = int.MinValue;
        FastMove bestMove = FastMove.Invalid;

        foreach (var move in moves)
        {
            if (cancellationRequested)
                return (0, FastMove.Invalid);

            using (diagnostics.MeasureApply())
                node.DoMove(move);

            bool isKingVulnerable;
            using (diagnostics.MeasureMoveValidate())
                isKingVulnerable = node.IsChecking(node.currentMove);

            if (isKingVulnerable)
            {
                diagnostics.invalidMoves++;
                using (diagnostics.MeasureApply())
                    node.UndoMove(move);
                continue;
            }

            isTerminal = false;
            (int currentValue, FastMove _) = Search(node, searchDepth - 1, plyFromRoot + 1, -beta, -alpha, -color);

            if (previousOrderingEnabled && plyFromRoot == 0)
                previousScores[move] = currentValue;

            using (diagnostics.MeasureApply())
                node.UndoMove(move);

            currentValue = -currentValue;
            if (currentValue > value)
            {
                bestMove = move;
                value = currentValue;
            }
            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
                break;
        }

        if (isTerminal)
        {
            value = color * EvaluateTerminalBoard(node, plyFromRoot);
        }

        return (value, bestMove);
    }

    int QuiescenceSearch(FastBoardNode node, int plyFromRoot, int alpha, int beta, int color)
    {
        if (!quiescenceSearchEnabled)
            return color * EvaluateMaybeTerminalBoard(node, plyFromRoot);

        var moves = new List<FastMove>(10);
        using (diagnostics.MeasureMoveGen())
            node.AddAllPossibleMoves(moves, node.currentMove, generateQuiet: false);

        using (diagnostics.MeasureMoveSort())
            OrderMoves(node, moves, -1);

        bool maybeTerminal = true;
        int value = int.MinValue;

        foreach (var move in moves)
        {
            if (cancellationRequested)
                return 0;

            using (diagnostics.MeasureApply())
                node.DoMove(move);

            bool isKingVulnerable;
            using (diagnostics.MeasureMoveValidate())
                isKingVulnerable = node.IsChecking(node.currentMove);
            if (isKingVulnerable)
            {
                diagnostics.invalidMoves++;
                using (diagnostics.MeasureApply())
                    node.UndoMove(move);
                continue;
            }

            maybeTerminal = false;
            int currentValue = QuiescenceSearch(node, plyFromRoot + 1, -beta, -alpha, -color);

            using (diagnostics.MeasureApply())
                node.UndoMove(move);

            currentValue = -currentValue;
            if (currentValue > value)
            {
                value = currentValue;
            }
            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
                break;
        }

        if (maybeTerminal)
        {
            return color * EvaluateMaybeTerminalBoard(node, plyFromRoot);
        }

        return value;
    }

    #endregion

    #region Move Ordering

    private void OrderMoves(FastBoardNode node, List<FastMove> moves, int plyFromRoot)
    {
        if (previousOrderingEnabled && plyFromRoot == 0 && previousScores.Count > 0)
        {
            moves.Sort((FastMove a, FastMove b) =>
            {
                previousScores.TryGetValue(a, out int aValue);
                previousScores.TryGetValue(b, out int bValue);
                return bValue - aValue; // descending
            });
        }
        else
        {
            moves.Sort((FastMove a, FastMove b) =>
            {
                int valueA = MoveValuer(node, a);
                int valueB = MoveValuer(node, b);
                return (valueB - valueA); // descending
            });
        }
    }

    private static int MoveValuer(FastBoardNode node, FastMove move)
    {
        int value = 1000; // Start high to always exceed invalid move scores of 0

        var attacker = node[move.start];
        int attackerValue = GetPieceValue(attacker.piece);

        // In general, value doing things with lower value pieces
        value -= attackerValue;

        if (move.moveType == MoveType.Move)
        {
            // TODO: devalue moving into threatened hexes
            // TODO: value moving threatened pieces
            // TODO: what else?
        }
        else if (move.moveType == MoveType.Attack)
        {
            var victim = node[move.target];

            int attackValue = GetPieceValue(victim.piece) - attackerValue;

            value += attackValue;
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
        return EvaluateMaybeTerminalBoard(node, plyFromRoot);
    }

    public int EvaluateMaybeTerminalBoard(FastBoardNode node, int plyFromRoot)
    {
        diagnostics.boardEvaluations++;

        using (diagnostics.MeasureEval())
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

            using (diagnostics.MeasureEvalThreats())
            {
                evaluationData.Prepare(node);
                boardValue += evaluationData.WhiteThreats.Count - evaluationData.BlackThreats.Count;
                boardValue += (evaluationData.WhitePawnThreats.Count * 2) - (evaluationData.BlackPawnThreats.Count * 2);
            }

            return boardValue;
        }
    }

    static int GetPieceValue(FastPiece piece)
    {
        switch (piece)
        {
            case FastPiece.King:
                return 900;

            case FastPiece.Queen:
                return 90;

            case FastPiece.Rook:
                return 40;

            case FastPiece.Knight:
                return 30;

            case FastPiece.Bishop:
                return 50;

            case FastPiece.Squire:
                return 20;

            case FastPiece.Pawn:
            default:
                return 10;
        }
    }

    #endregion

    public void CancelMove()
    {
        cancellationRequested = true;
    }

    public IEnumerable<string> GetDiagnosticInfo()
    {
        yield return $"Cancellation requested: {cancellationRequested}";
        yield return $"bestMove: {bestMove}";
        yield return $"currentDepth: {currentDepth}";
    }


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
        public readonly Stopwatch moveGenTimer = new Stopwatch();
        public readonly Stopwatch moveSortTimer = new Stopwatch();
        public readonly Stopwatch moveValidateTimer = new Stopwatch();
        public readonly Stopwatch evalTimer = new Stopwatch();
        public readonly Stopwatch quiescenceTimer = new Stopwatch();
        public readonly Stopwatch evalThreatsTimer = new Stopwatch();
        public readonly Stopwatch applyTimer = new Stopwatch();
        public readonly Stopwatch getMoveTimer = new Stopwatch();

        public int boardEvaluations = 0;
        public int terminalBoardEvaluations = 0;
        public int invalidMoves = 0;

        public void Reset()
        {
            moveGenTimer.Reset();
            moveSortTimer.Reset();
            moveValidateTimer.Reset();
            evalTimer.Reset();
            evalThreatsTimer.Reset();
            applyTimer.Reset();
            getMoveTimer.Reset();
            boardEvaluations = 0;
            invalidMoves = 0;
        }

        public Measurer MeasureMoveGen() => new Measurer(moveGenTimer);
        public Measurer MeasureMoveSort() => new Measurer(moveSortTimer);
        public Measurer MeasureMoveValidate() => new Measurer(moveValidateTimer);
        public Measurer MeasureQuiescence() => new Measurer(quiescenceTimer);
        public Measurer MeasureEval() => new Measurer(evalTimer);
        public Measurer MeasureEvalThreats() => new Measurer(evalThreatsTimer);
        public Measurer MeasureApply() => new Measurer(applyTimer);

        public struct Measurer : IDisposable
        {
            Stopwatch stopwatch;
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
