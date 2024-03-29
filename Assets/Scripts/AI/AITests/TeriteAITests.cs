using NUnit.Framework;
using System.Linq;
using Extensions;
using Unity.PerformanceTesting;
using System;
using System.Collections.Generic;
using UnityEngine.TestTools;

public class TeriteAITests
{
    static readonly Team[] Teams = new Team[] { Team.White, Team.Black };

    static readonly int[] Depths = new int[] { 3, 4 };

    static FastBoardNode CreateBoardNode(Team toMove, (Team team, Piece piece, Index location)[] pieces)
    {
        var piecePositions = new BidirectionalDictionary<(Team team, Piece piece), Index>();
        foreach (var piece in pieces)
            piecePositions.Add((piece.team, piece.piece), piece.location);

        var state = new BoardState(piecePositions, toMove, Team.None, Team.None, 0);
        return new FastBoardNode(state, null);
    }

    [Test]
    public void MateInOne_Test1([ValueSource(nameof(Teams))] Team attacker)
    {
        var ai = new TeriteAI();
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (defender, Piece.King, new Index(8, 'I')),
            (attacker, Piece.KingsRook, new Index(2, 'G')),
            (attacker, Piece.QueensRook, new Index(1, 'H')),
        });

        var foundMove = ai.GetMove(board);
        var expected = new FastMove(new Index(2, 'G'), new Index(2, 'I'), MoveType.Move);
        Assert.AreEqual(expected, foundMove);

        LogDiagnostics(ai.diagnostics);
    }

    [Test]
    public void MateInOne_Test2([ValueSource(nameof(Teams))] Team attacker)
    {
        var ai = new TeriteAI();
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (defender, Piece.King, new Index(5, 'A')),
            (attacker, Piece.Queen, new Index(1, 'C')),
        });

        var foundMove = ai.GetMove(board);
        var expected = new FastMove(new Index(1, 'C'), new Index(5, 'C'), MoveType.Move);
        Assert.AreEqual(expected, foundMove);
    }

    [Test]
    public void MateInOne_Test3([ValueSource(nameof(Teams))] Team attacker)
    {
        var ai = new TeriteAI();
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (attacker, Piece.KingsRook, new Index(1, 'D')),
            (attacker, Piece.QueensRook, new Index(1, 'F')),
            (attacker, Piece.Queen, new Index(3, 'A')),
            (defender, Piece.King, new Index(9, 'E')),
            (defender, Piece.Pawn1, new Index(8, 'E')),
            (defender, Piece.Pawn2, new Index(8, 'B')),
            (defender, Piece.Pawn3, new Index(8, 'H')),
        });

        var foundMove = ai.GetMove(board);
        var expected = new FastMove(new Index(3, 'A'), new Index(9, 'A'), MoveType.Move);
        Assert.AreEqual(expected, foundMove);

        board.DoMove(foundMove);
        Assert.True(board.IsChecking(attacker));
        Assert.False(board.HasAnyValidMoves(defender));
    }

    [Test]
    public void MateInOne_Defend([ValueSource(nameof(Teams))] Team attacker)
    {
        var ai = new TeriteAI();
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.KingsRook, new Index(1, 'H')),
            (attacker, Piece.QueensRook, new Index(1, 'I')),
            (attacker, Piece.King, new Index(2, 'I')),
            (defender, Piece.King, new Index(9, 'I')),
        });

        var foundMove = ai.GetMove(board);
        var expected = new FastMove(new Index(1, 'I'), new Index(2, 'I'), MoveType.Defend);
        Assert.AreEqual(expected, foundMove);

        board.DoMove(foundMove);
        Assert.True(board.IsChecking(attacker));
        Assert.False(board.HasAnyValidMoves(defender));
    }

    [Test]
    public void MateInOne_EnPassant()
    {
        var ai = new TeriteAI(1);
        Team attacker = Team.White;
        Team defender = attacker.Enemy();

        var board = CreateBoardNode(defender, new[]
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (attacker, Piece.KingsBishop, new Index(5, 'E')),
            (attacker, Piece.QueensBishop, new Index(7, 'E')),
            (attacker, Piece.QueensKnight, new Index(5, 'G')),
            (attacker, Piece.Pawn1, new Index(7, 'G')),
            (defender, Piece.King, new Index(8, 'I')),
            (defender, Piece.Pawn1, new Index(9, 'H')),
        });

        // Double move
        board.DoMove(new FastMove(new Index(9, 'H'), new Index(7, 'H'), MoveType.Move));

        var foundMove = ai.GetMove(board);
        var expected = new FastMove(new Index(7, 'G'), new Index(8, 'H'), MoveType.EnPassant);
        Assert.AreEqual(expected, foundMove);

        board.DoMove(foundMove);
        Assert.True(board.IsChecking(attacker));

        Assert.AreEqual(Team.Black, board.currentMove);
        var validMoves = board.GetAllValidMoves().ToArray();
        Assert.False(board.HasAnyValidMoves(defender));
    }

    [Test]
    public void MateInTwo_Test1([ValueSource(nameof(Teams))] Team attacker)
    {
        var ai = new TeriteAI();
        Team defender = attacker.Enemy();
        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (attacker, Piece.Queen, new Index(10, 'F')),
            (defender, Piece.King, new Index(6, 'H')),
        });

        var foundMove = ai.GetMove(board);
        FastMove expected = new FastMove(new Index(10, 'F'), new Index(6, 'F'), MoveType.Move);
        Assert.AreEqual(expected, foundMove);
        board.DoMove(expected);

        // Defender can only retreat to I5 or I6
        var defenderMoves = board.GetAllValidMoves().ToArray();
        Assert.AreEqual(new[] {
            new FastMove(new Index(6, 'H'), new Index(6, 'I'), MoveType.Move),
            new FastMove(new Index(6, 'H'), new Index(5, 'I'), MoveType.Move),
        }, defenderMoves);
        board.DoMove(new FastMove(new Index(6, 'H'), new Index(6, 'I'), MoveType.Move));

        // should find mate
        foundMove = ai.GetMove(board);
        expected = new FastMove(new Index(6, 'F'), new Index(6, 'G'), MoveType.Move);
        Assert.AreEqual(expected, foundMove);
    }

    [Test]
    public void MateInTwo_Test2([ValueSource(nameof(Teams))] Team attacker)
    {
        var ai = new TeriteAI();
        Team defender = attacker.Enemy();

        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'G')),
            (attacker, Piece.Queen, new Index(7, 'C')),
            (attacker, Piece.WhiteSquire, new Index(6, 'E')),
            (defender, Piece.King, new Index(8, 'I')),
        });

        var foundMove = ai.GetMove(board);
        // FastMove expected = new FastMove(new Index(10, 'F'), new Index(6, 'F'), MoveType.Move);
        // Assert.AreEqual(expected, foundMove);
        board.DoMove(foundMove);

        var defenderMoves = board.GetAllValidMoves().ToArray();
        // Assert.AreEqual(new[] {
        //     new FastMove(new Index(6, 'H'), new Index(6, 'I'), MoveType.Move),
        //     new FastMove(new Index(6, 'H'), new Index(5, 'I'), MoveType.Move),
        // }, defenderMoves);
        board.DoMove(defenderMoves[0]);

        // should find mate
        foundMove = ai.GetMove(board);
        // expected = new FastMove(new Index(6, 'F'), new Index(6, 'G'), MoveType.Move);
        // Assert.AreEqual(expected, foundMove);

        board.DoMove(foundMove);
        Assert.True(board.IsChecking(attacker));
        Assert.False(board.HasAnyValidMoves(defender));
    }

    [Test]
    public void MateInThree_One()
    {
        var ai = new TeriteAI(maxSearchDepth: 6);
        Team attacker = Team.White;
        Team defender = attacker.Enemy();

        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'G')),
            (attacker, Piece.Queen, new Index(3, 'C')),

            (attacker, Piece.KingsRook, new Index(1, 'A')),
            (attacker, Piece.QueensRook, new Index(2, 'I')),

            (attacker, Piece.Pawn1, new Index(4, 'A')),
            (attacker, Piece.Pawn2, new Index(4, 'B')),
            (attacker, Piece.Pawn3, new Index(4, 'C')),
            (attacker, Piece.Pawn4, new Index(3, 'D')),
            (attacker, Piece.Pawn5, new Index(4, 'F')),
            (attacker, Piece.Pawn6, new Index(3, 'G')),
            (attacker, Piece.Pawn7, new Index(3, 'H')),
            (attacker, Piece.Pawn8, new Index(4, 'I')),

            (attacker, Piece.BlackSquire, new Index(1, 'E')),
            (attacker, Piece.WhiteSquire, new Index(6, 'G')),
            (attacker, Piece.GraySquire, new Index(5, 'I')),

            (attacker, Piece.KingsBishop, new Index(2, 'F')),
            (attacker, Piece.QueensBishop, new Index(2, 'D')),
            (attacker, Piece.QueensKnight, new Index(9, 'C')),

            (defender, Piece.King, new Index(8, 'D')),
            (defender, Piece.GraySquire, new Index(10, 'D')),
        });

        board.DoMove(new FastMove(new Index(3, 'C'), new Index(4, 'D'), MoveType.Move));
        board.DoMove(new FastMove(new Index(8, 'D'), new Index(7, 'C'), MoveType.Move));

        board.DoMove(new FastMove(new Index(4, 'D'), new Index(10, 'D'), MoveType.Attack));
        board.DoMove(new FastMove(new Index(7, 'C'), new Index(8, 'C'), MoveType.Move));

        ai.GetMove(board);

        board.DoMove(new FastMove(new Index(6, 'G'), new Index(6, 'E'), MoveType.Move));
        board.DoMove(new FastMove(new Index(8, 'C'), new Index(7, 'C'), MoveType.Move));



        board.DoMove(new FastMove(new Index(10, 'D'), new Index(8, 'D'), MoveType.Move));

        // W Queen C3 -> D4 (check)
        // B King D8 -> C7

        // W Queen D4 -> D10 (capture)
        // B King C7 -> C8

        // W Squire G6 -> E6
        // B King C8 -> C7

        // W Queen D10 -> D8 (checkmate)
    }

    [Test]
    public void MateInOne_Promotion()
    {
        var ai = new TeriteAI();
        Team attacker = Team.White;
        Team defender = attacker.Enemy();

        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (attacker, Piece.Pawn1, new Index(8, 'C')),
            (defender, Piece.King, new Index(9, 'A')),
        });

        var foundMove = ai.GetMove(board);
        // Promote to queen
        FastMove expected = new FastMove(new Index(8, 'C'), new Index(9, 'C'), MoveType.Move, FastPiece.Queen);
        Assert.AreEqual(expected, foundMove);
    }

    [Test]
    public void EvaluationValuesPromotion([ValueSource(nameof(Teams))]Team toMove)
    {
        var ai = new TeriteAI();
        Team attacker = toMove;
        Team defender = attacker.Enemy();
        int perspective = attacker == Team.White ? 1 : -1;

        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (defender, Piece.King, new Index(3, 'A')),
            (attacker, Piece.Pawn1, new Index(9, 'H')),
        });

        ai.evaluationData.Prepare(board);
        var value1 = ai.EvaluateBoard(board, 1) * perspective;

        // Promote to queen
        board.DoMove(new FastMove(new Index(9, 'H'), new Index(10, 'H'), MoveType.Move, FastPiece.Queen));
        Assert.AreEqual((attacker, FastPiece.Queen), board[new Index(10, 'H')]);

        ai.evaluationData.Prepare(board);
        var value2 = ai.EvaluateBoard(board, 1) * perspective;
        Assert.Greater(value2, value1);
    }

    [Test]
    public void EvaluationValuesFreedom([ValueSource(nameof(Teams))]Team toMove)
    {
        var ai = new TeriteAI();
        Team attacker = toMove;
        Team defender = attacker.Enemy();
        int perspective = attacker == Team.White ? 1 : -1;

        var board1 = CreateBoardNode(attacker, new[] // lots of room to move
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (attacker, Piece.BlackSquire, new Index(5, 'E')),
            (defender, Piece.King, new Index(9, 'I')),
        });

        var board2 = CreateBoardNode(attacker, new[] // fewer moves, against the side
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (attacker, Piece.BlackSquire, new Index(5, 'A')),
            (defender, Piece.King, new Index(9, 'I')),
        });

        ai.evaluationData.Prepare(board1);
        var value1 = ai.EvaluateBoard(board1, 1) * perspective;

        ai.evaluationData.Prepare(board2);
        var value2 = ai.EvaluateBoard(board2, 1) * perspective;
        Assert.Greater(value1, value2);
    }

    // [Test] Skipped because specific piece valuation is in flux
    public void DoesValuePawnAdvancing([ValueSource(nameof(Teams))]Team toMove)
    {
        var ai = new TeriteAI();

        Team attacker = toMove;
        Team defender = attacker.Enemy();
        int perspective = attacker == Team.White ? 1 : -1;

        Index pawnPos;
        Index nextPawnPos;
        if (toMove == Team.White)
        {
            pawnPos = new Index(8, 'H');
            nextPawnPos = new Index(9, 'H');
        }
        else
        {
            pawnPos = new Index(3, 'H');
            nextPawnPos = new Index(2, 'H');
        }

        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (defender, Piece.King, new Index(3, 'A')),
            (attacker, Piece.Pawn1, pawnPos),
        });

        var value1 = ai.EvaluateBoard(board, 1) * perspective;
        board.DoMove(new FastMove(pawnPos, nextPawnPos, MoveType.Move));
        Assert.AreEqual((attacker, FastPiece.Pawn), board[nextPawnPos]);

        var value2 = ai.EvaluateBoard(board, 1) * perspective;
        Assert.Greater(value2, value1);
    }

    [Test, Performance]
    public void Performance_Depth([ValueSource(nameof(Depths))] int searchDepth, [ValueSource(nameof(Teams))] Team toMove)
    {
        var ai = new TeriteAI(searchDepth);

        Team attacker = toMove;
        Team defender = attacker.Enemy();

        var board = CreateBoardNode(attacker, new[]
        {
            (Team.White, Piece.Pawn1, new Index(2, 'A')),
            (Team.White, Piece.Pawn2, new Index(3, 'B')),
            (Team.White, Piece.Pawn3, new Index(2, 'C')),
            (Team.White, Piece.Pawn4, new Index(3, 'D')),
            (Team.White, Piece.Pawn5, new Index(3, 'F')),
            (Team.White, Piece.Pawn6, new Index(2, 'G')),
            (Team.White, Piece.Pawn7, new Index(3, 'H')),
            (Team.White, Piece.Pawn8, new Index(2, 'I')),
            (Team.White, Piece.QueensRook, new Index(1, 'A')),
            (Team.White, Piece.QueensKnight, new Index(2, 'B')),
            (Team.White, Piece.Queen, new Index(1, 'C')),
            (Team.White, Piece.QueensBishop, new Index(2, 'D')),
            (Team.White, Piece.WhiteSquire, new Index(1, 'E')),
            (Team.White, Piece.KingsBishop, new Index(2, 'F')),
            (Team.White, Piece.King, new Index(1, 'G')),
            (Team.White, Piece.KingsKnight, new Index(2, 'H')),
            (Team.White, Piece.KingsRook, new Index(1, 'I')),
            (Team.White, Piece.GraySquire, new Index(2, 'E')),
            (Team.White, Piece.BlackSquire, new Index(3, 'E')),

            (Team.Black, Piece.Pawn1, new Index(8, 'A')),
            (Team.Black, Piece.Pawn2, new Index(8, 'B')),
            (Team.Black, Piece.Pawn3, new Index(8, 'C')),
            (Team.Black, Piece.Pawn4, new Index(8, 'D')),
            (Team.Black, Piece.Pawn5, new Index(8, 'F')),
            (Team.Black, Piece.Pawn6, new Index(8, 'G')),
            (Team.Black, Piece.Pawn7, new Index(8, 'H')),
            (Team.Black, Piece.Pawn8, new Index(8, 'I')),
            (Team.Black, Piece.QueensRook, new Index(9, 'A')),
            (Team.Black, Piece.QueensKnight, new Index(9, 'B')),
            (Team.Black, Piece.Queen, new Index(9, 'C')),
            (Team.Black, Piece.QueensBishop, new Index(9, 'D')),
            (Team.Black, Piece.WhiteSquire, new Index(9, 'E')),
            (Team.Black, Piece.KingsBishop, new Index(9, 'F')),
            (Team.Black, Piece.King, new Index(9, 'G')),
            (Team.Black, Piece.KingsKnight, new Index(9, 'H')),
            (Team.Black, Piece.KingsRook, new Index(9, 'I')),
            (Team.Black, Piece.GraySquire, new Index(8, 'E')),
            (Team.Black, Piece.BlackSquire, new Index(7, 'E')),
        });

        Measure.Method(() => ai.GetMove(board))
            // .WarmupCount(10)
            // .MeasurementCount(10)
            .IterationsPerMeasurement(5)
            .GC() // collect gc info
            .Run();

        LogDiagnostics(ai.diagnostics);
    }

    [Test]
    public void QuiescenceSearchTest([ValueSource(nameof(Teams))] Team attacker)
    {
        var ai = new TeriteAI(maxSearchDepth: 1);
        Team defender = attacker.Enemy();

        var board = CreateBoardNode(attacker, new[]
        {
            (attacker, Piece.King, new Index(1, 'A')),
            (attacker, Piece.Queen, new Index(3, 'E')),
            (defender, Piece.QueensBishop, new Index(9, 'E')),
            (defender, Piece.QueensRook, new Index(9, 'C')),
            (defender, Piece.King, new Index(9, 'I')),
        });

        var foundMove = ai.GetMove(board);
        Assert.That(foundMove, Is.Not.EqualTo(new FastMove(new Index(3, 'E'), new Index(9, 'E'), MoveType.Attack)));
    }

    private void LogDiagnostics(TeriteAI.DiagnosticInfo ai)
    {
        string[] lines = {
            $"getMove: {ai.getMove}",
            $"Evaluated {ai.terminalBoardEvaluations:N0} TERMINAL board positions",
            $"Generated {ai.invalidMoves:N0} invalid moves",
            $"Cutoffs:  {ai.searchCutoff:N0} search, {ai.quiescenceCutoff:N0} quiescence",
            $"Generating moves: {ai.moveGen}",
            $"Sorting moves: {ai.moveSort}",
            $"Validating moves: {ai.moveValidate}",
            $"Applying moves: {ai.apply}",
            $"Quiescence search: {ai.quiescence}",
            $"  move gen: {ai.quiescenceMoveGen}",
            $"  move sort: {ai.quiescenceMoveSort}",
            $"  move validate: {ai.quiescenceMoveValidate}",
            $"  apply: {ai.quiescenceApply}",
            $"  eval: {ai.quiescenceEval}",
            $"    threats: {ai.evalThreats}",
        };

        UnityEngine.Debug.Log(string.Join("\n", lines));
    }

    static IEnumerable<MateInOneInfo> GetMateInOnes()
    {
        var attacker = Team.White;
        var defender = Team.Black;

        var info = new MateInOneInfo()
        {
            Name = "Rook smother",
            Board = CreateBoardNode(Team.White, new[]
            {
                (Team.White, Piece.King, new Index(1, 'A')),
                (Team.Black, Piece.King, new Index(8, 'I')),
                (Team.White, Piece.KingsRook, new Index(2, 'G')),
                (Team.White, Piece.QueensRook, new Index(1, 'H')),
            }),
            ExpectedMove = new FastMove(new Index(2, 'G'), new Index(2, 'I'), MoveType.Move),
            ToMove = Team.White,
        };
        yield return info;
        yield return info.Invert();

        info = new MateInOneInfo()
        {
            Name = "Queen on side",
            Board = CreateBoardNode(attacker, new[]
            {
                (attacker, Piece.King, new Index(1, 'A')),
                (defender, Piece.King, new Index(5, 'A')),
                (attacker, Piece.Queen, new Index(1, 'C')),
            }),
            ToMove = Team.White,
            ExpectedMove = new FastMove(new Index(1, 'C'), new Index(5, 'C'), MoveType.Move),
        };
        yield return info;
        yield return info.Invert();

        /* Disabled because the best move sometimes changes to Move(A3 -> A7)
        info = new MateInOneInfo()
        {
            Name = "Complex",
            Board = CreateBoardNode(attacker, new[]
            {
                (attacker, Piece.King, new Index(1, 'A')),
                (attacker, Piece.KingsRook, new Index(1, 'D')),
                (attacker, Piece.QueensRook, new Index(1, 'F')),
                (attacker, Piece.Queen, new Index(3, 'A')),
                (defender, Piece.King, new Index(9, 'E')),
                (defender, Piece.Pawn1, new Index(8, 'E')),
            }),
            ExpectedMove = new FastMove(new Index(3, 'A'), new Index(7, 'I'), MoveType.Move),
            ToMove = attacker,
        };
        yield return info;
        yield return info.Invert();
        */

        info = new MateInOneInfo()
        {
            Name = "Defend",
            Board = CreateBoardNode(attacker, new[]
            {
                (attacker, Piece.King, new Index(1, 'A')),
                (attacker, Piece.KingsRook, new Index(1, 'H')),
                (attacker, Piece.QueensRook, new Index(1, 'I')),
                (attacker, Piece.GraySquire, new Index(2, 'I')),
                (attacker, Piece.Pawn1, new Index(2, 'G')),
                (defender, Piece.King, new Index(9, 'I')),
            }),
            ExpectedMove = new FastMove(new Index(1, 'I'), new Index(2, 'I'), MoveType.Defend),
        };
        yield return info;
        yield return info.Invert();
    }

    [Test]
    public void MateInOneTest([ValueSource(nameof(GetMateInOnes))]MateInOneInfo info)
    {
        var ai = new TeriteAI();

        Team attacker = info.ToMove;
        Team defender = attacker.Enemy();

        var foundMove = ai.GetMove(info.Board);

        info.Board.DoMove(foundMove);

        Assert.True(info.Board.IsChecking(attacker));
        Assert.False(info.Board.HasAnyValidMoves(defender));
        Assert.AreEqual(info.ExpectedMove, foundMove);
    }

    [UnityTest, Performance]
    public System.Collections.IEnumerator PlayGameVsRandom([ValueSource(nameof(Teams))] Team playAs)
    {
        var game = Game.CreateNewGame();
        IHexAI teriteAI = new TeriteAI();
        IHexAI randomAI = new RandomAI(seed: 1337);

        List<double> moveLengths = new List<double>();

        int i = 0;
        while (game.winner == Winner.Pending)
        {
            var toMove = game.GetCurrentTurn();
            var ai = toMove == playAs ? teriteAI : randomAI;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var moveTask = ai.GetMove(game);

            while (!moveTask.IsCompleted)
                yield return null;

            var move = moveTask.Result;
            var elapsed = sw.Elapsed;

            if (toMove == playAs)
                moveLengths.Add(elapsed.TotalMilliseconds);

            // if (elapsed.TotalMilliseconds > 4000)
            // {
            //     UnityEngine.Debug.LogWarning($"Move took a while!!!");
            //     LogBoard(game);
            // }

            string moveStr = move.ToString(game);

            move.ApplyTo(game);
            UnityEngine.Debug.Log($"{game.GetTurnCount()}: {toMove} played {moveStr} after {Math.Round(elapsed.TotalMilliseconds)} ms");

            if (i++ > 100)
                throw new Exception("Game went on too long");
        }

        if (playAs == Team.White)
            Assert.AreEqual(Winner.White, game.winner, "TeriteAI did not win");
        else
            Assert.AreEqual(Winner.Black, game.winner, "TeriteAI did not win");

        UnityEngine.Debug.Log($"After {game.GetTurnCount()} turns, endType:{game.endType} winner:{game.winner}");

        var avgLength = moveLengths.Average();
        var maxLength = moveLengths.Max();
        UnityEngine.Debug.Log($"Avg: {avgLength:.#}ms, max:{maxLength:.#}ms");
    }

    [Test, Performance]
    public void SlowMove()
    {
        var ai = new TeriteAI();
        var node = CreateBoardNode(Team.Black, new[]
        {
            (Team.White, Piece.King, new Index(3, 'G')),
            (Team.White, Piece.KingsRook, new Index(2, 'I')),
            (Team.White, Piece.KingsKnight, new Index(2, 'H')),
            (Team.White, Piece.KingsBishop, new Index(2, 'F')),
            (Team.White, Piece.QueensBishop, new Index(2, 'D')),
            (Team.White, Piece.WhiteSquire, new Index(1, 'E')),
            (Team.White, Piece.GraySquire, new Index(2, 'E')),
            (Team.White, Piece.Pawn1, new Index(2, 'A')),
            (Team.White, Piece.Pawn3, new Index(3, 'C')),
            (Team.White, Piece.Pawn4, new Index(5, 'D')),
            (Team.White, Piece.Pawn5, new Index(4, 'F')),
            (Team.Black, Piece.QueensKnight, new Index(5, 'G')),
            (Team.White, Piece.Pawn8, new Index(3, 'I')),
            (Team.Black, Piece.King, new Index(9, 'G')),
            (Team.Black, Piece.Queen, new Index(9, 'C')),
            (Team.Black, Piece.KingsRook, new Index(9, 'I')),
            (Team.Black, Piece.QueensRook, new Index(9, 'A')),
            (Team.Black, Piece.KingsKnight, new Index(7, 'D')),
            (Team.Black, Piece.KingsBishop, new Index(9, 'F')),
            (Team.Black, Piece.QueensBishop, new Index(9, 'D')),
            (Team.Black, Piece.WhiteSquire, new Index(7, 'E')),
            (Team.Black, Piece.GraySquire, new Index(8, 'E')),
            (Team.Black, Piece.BlackSquire, new Index(9, 'E')),
            (Team.Black, Piece.Pawn1, new Index(8, 'A')),
            (Team.Black, Piece.Pawn2, new Index(8, 'B')),
            (Team.Black, Piece.Pawn3, new Index(8, 'C')),
            (Team.Black, Piece.Pawn4, new Index(6, 'D')),
            (Team.Black, Piece.Pawn5, new Index(8, 'F')),
            (Team.Black, Piece.Pawn6, new Index(8, 'G')),
            (Team.Black, Piece.Pawn7, new Index(6, 'H')),
            (Team.Black, Piece.Pawn8, new Index(7, 'I')),
        });

        var move = ai.GetMove(node);
        UnityEngine.Debug.Log(move);
        LogDiagnostics(ai.diagnostics);
    }

    static void LogBoard(Game game)
    {
        var state = game.GetCurrentBoardState();
        var lines = new List<string>();

        foreach (var kvp in state.allPiecePositions)
        {
            var team = kvp.Key.team;
            var piece = kvp.Key.piece;
            var position = kvp.Value;
            lines.Add($"(Team.{team}, Piece.{piece}, new Index({position.GetNumber()}, '{position.GetLetter()}')),");
        }

        foreach (var p in game.promotions)
        {
            lines.Add($"new Promotion(Team.{p.team}, Piece.{p.from}, Piece.{p.to}, {p.turnNumber}),");
        }

        UnityEngine.Debug.Log($"PRINT BOARD\n{string.Join("\n", lines)}");
    }
}

public class MateInOneInfo
{
    public string Name = "Unnamed";
    public FastBoardNode Board;
    public Team ToMove = Team.White;
    public FastMove ExpectedMove;
    public MateInOneInfo Invert()
    {
        var invertedBoard = new FastBoardNode();
        invertedBoard.currentMove = Board.currentMove.Enemy();
        for (int i = 0; i < Board.positions.Length; i++)
        {
            var index = FastIndex.FromByte((byte)i);
            var newIndex = index.Mirror();
            var piece = Board[index];
            if (piece.team != Team.None)
                piece.team = piece.team.Enemy();
            invertedBoard[newIndex] = piece;

            if (piece.piece == FastPiece.King)
            {
                if (piece.team == Team.White)
                {
                    invertedBoard.whiteKing = newIndex;
                }
                else
                {
                    invertedBoard.blackKing = newIndex;
                }
            }
        }

        var invertedMove = new FastMove(ExpectedMove.start.Mirror(), ExpectedMove.target.Mirror(), ExpectedMove.moveType, ExpectedMove.promoteTo);
        return new MateInOneInfo()
        {
            Name = Name,
            Board = invertedBoard,
            ToMove = ToMove.Enemy(),
            ExpectedMove = invertedMove,
        };
    }

    public override string ToString()
    {
        return $"{Name} (as {ToMove})";
    }
}

