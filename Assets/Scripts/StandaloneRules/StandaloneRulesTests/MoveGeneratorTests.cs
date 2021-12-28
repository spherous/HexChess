using NUnit.Framework;
using System.Linq;

public class MoveGeneratorTests
{
    [Test]
    public void EnPassantTest()
    {
        Index attackerLoc = new Index(4, 'C');
        Index victimLoc = new Index(5, 'D');
        Index target = new Index(4, 'D');

        var piecePositions = new BidirectionalDictionary<(Team, Piece), Index>();
        piecePositions.Add(victimLoc, (Team.White, Piece.Pawn4));
        piecePositions.Add(attackerLoc, (Team.Black, Piece.Pawn1));
        var state = new BoardState(piecePositions, Team.Black, Team.None, Team.None, 0);

        // var allmoves = MoveGenerator.GetAllPossiblePawnMoves(attackerLoc, Team.Black, state).ToArray();
        var allmoves = MoveGenerator.GetAllPossibleMoves(attackerLoc, Piece.Pawn1, Team.Black, state, Enumerable.Empty<Promotion>()).ToArray();
        Assert.AreEqual((target, MoveType.EnPassant), allmoves[0]);
        Assert.AreEqual((attackerLoc.GetNeighborAt(HexNeighborDirection.Down).Value, MoveType.Move), allmoves[1]);
        Assert.AreEqual(2, allmoves.Length);

        // Cannot en passant when target is occupied
        piecePositions.Add(target, (Team.White, Piece.Queen));
        // allmoves = MoveGenerator.GetAllPossiblePawnMoves(attackerLoc, Team.Black, state).ToArray();
        allmoves = MoveGenerator.GetAllPossibleMoves(attackerLoc, Piece.Pawn1, Team.Black, state, Enumerable.Empty<Promotion>()).ToArray();
        Assert.AreEqual((target, MoveType.Attack), allmoves[0]);
        Assert.AreEqual((attackerLoc.GetNeighborAt(HexNeighborDirection.Down).Value, MoveType.Move), allmoves[1]);
        Assert.AreEqual(2, allmoves.Length);
    }

    [Test]
    public void ValidateEnPassantTest()
    {
        // White pawn C2 -> C4
        var state1 = TestUtils.CreateBoardState(Team.White, new[] {
            (new Index(9, 'I'), Team.Black, Piece.King),
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(2, 'C'), Team.White, Piece.Pawn3),
            (new Index(4, 'B'), Team.Black, Piece.Pawn1),
        });
        // Black pawn on B4 can enpassant -> C3
        var state2 = TestUtils.CreateBoardState(Team.Black, new[] {
            (new Index(9, 'I'), Team.Black, Piece.King),
            (new Index(1, 'A'), Team.White, Piece.King),
            (new Index(4, 'C'), Team.White, Piece.Pawn3),
            (new Index(4, 'B'), Team.Black, Piece.Pawn1),
        });

        var enPassantMove = (new Index(4, 'B'), new Index(3, 'C'), MoveType.EnPassant, Piece.Pawn1);
        var moves = MoveGenerator.GenerateAllValidMoves(Team.Black, null, state2, state1).ToArray();
        Assert.That(moves, Has.Member(enPassantMove));

        moves = MoveGenerator.GenerateAllValidMoves(Team.Black, null, state2, state2).ToArray();
        Assert.That(moves, Has.No.Member(enPassantMove));
    }
}
