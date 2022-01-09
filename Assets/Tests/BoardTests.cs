using System.Collections;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class BoardTests
{
    [UnityTest]
    public IEnumerator KingOnlyTest()
    {
        SceneManager.LoadScene("Scenes/SandboxMode");
        yield return null;
        var board = GameObject.FindObjectOfType<Board>();
        Assert.AreEqual(38, board.activePieces.Count);

        var piecePositions = new BidirectionalDictionary<(Team, Piece), Index>();
        piecePositions[(Team.White, Piece.King)] = new Index(5, 'E');
        board.SetBoardState(new BoardState()
        {
            allPiecePositions = piecePositions,
            check = Team.None,
            checkmate = Team.None,
            currentMove = Team.White,
            executedAtTime = 1,
        });

        Assert.AreEqual(1, board.activePieces.Count);
        IPiece piece = board.activePieces[(Team.White, Piece.King)];
        var kingMoves = MoveGenerator
            .GetAllPossibleMoves(piece.location, piece.piece, piece.team, board.GetCurrentBoardState(), board.currentGame.promotions)
            .ToArray();

        Assert.AreEqual(6, kingMoves.Length);
        // ORDER {Up, UpRight, DownRight, Down, DownLeft, UpLeft};
        Assert.AreEqual((new Index(6, 'E'), MoveType.Move), kingMoves[0]);
        Assert.AreEqual((new Index(6, 'F'), MoveType.Move), kingMoves[1]);
        Assert.AreEqual((new Index(5, 'F'), MoveType.Move), kingMoves[2]);
        Assert.AreEqual((new Index(4, 'E'), MoveType.Move), kingMoves[3]);
        Assert.AreEqual((new Index(5, 'D'), MoveType.Move), kingMoves[4]);
        Assert.AreEqual((new Index(6, 'D'), MoveType.Move), kingMoves[5]);
    }

    [UnityTest]
    public IEnumerator KingCheckDetectionTest()
    {
        SceneManager.LoadScene("Scenes/SandboxMode");
        yield return null;
        var board = GameObject.FindObjectOfType<Board>();
        Assert.AreEqual(38, board.activePieces.Count);

        var piecePositions = new BidirectionalDictionary<(Team, Piece), Index>();
        piecePositions[(Team.Black, Piece.King)] = new Index(5, 'E');
        piecePositions[(Team.White, Piece.KingsRook)] = new Index(1, 'E');
        piecePositions[(Team.White, Piece.King)] = new Index(1, 'A');
        var newState = new BoardState()
        {
            allPiecePositions = piecePositions,
            check = Team.None,
            checkmate = Team.None,
            currentMove = Team.Black,
            executedAtTime = 1,
        };
        board.SetBoardState(newState);
        board.currentGame.AddState(newState);
        // board.AdvanceTurn(newState, false);

        // Assert.True(board.IsChecking(board.GetCurrentBoardState(), Team.White));
        BoardState currentState = board.GetCurrentBoardState();
        Assert.True(board.currentGame.IsChecking(Team.White, currentState));
        Assert.False(board.currentGame.IsChecking(Team.Black, currentState));
    }

    [Test]
    public void EnPassantTest()
    {
        // White pawn moves to A4
        var piecePositions2 = new BidirectionalDictionary<(Team, Piece), Index>();
        piecePositions2[(Team.White, Piece.King)] = new Index(1, 'G');
        piecePositions2[(Team.Black, Piece.King)] = new Index(9, 'G');
        piecePositions2[(Team.White, Piece.Pawn1)] = new Index(4, 'A');
        piecePositions2[(Team.Black, Piece.Pawn1)] = new Index(4, 'B');
        var state2 = new BoardState()
        {
            allPiecePositions = piecePositions2,
            check = Team.None,
            checkmate = Team.None,
            currentMove = Team.Black,
            executedAtTime = 1,
        };

        // Black pawn should be able to capture white pawn by going to A3
        var pawn = new GameObject().AddComponent<Pawn>();
        pawn.Init(Team.Black, Piece.Pawn1, new Index(4, 'B'));
        var board = GameObject.FindObjectOfType<Board>();
        var moves = MoveGenerator.GetAllPossibleMoves(pawn.location, pawn.piece, pawn.team, state2, board.currentGame.promotions).ToList();

        Assert.AreEqual((new Index(3, 'A'), MoveType.EnPassant), moves[0]);
        Assert.AreEqual((new Index(3, 'B'), MoveType.Move), moves[1]);
    }
}