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
        piecePositions.Add(victimLoc, (Team.White, Piece.Pawn1));
        piecePositions.Add(attackerLoc, (Team.Black, Piece.Pawn1));
        var state = new BoardState(piecePositions, Team.Black, Team.None, Team.None, 0);

        var allmoves = MoveGenerator.GetAllPossiblePawnMoves(attackerLoc, Team.Black, state).ToArray();
        Assert.AreEqual((target, MoveType.EnPassant), allmoves[0]);
        Assert.AreEqual((attackerLoc.GetNeighborAt(HexNeighborDirection.Down).Value, MoveType.Move), allmoves[1]);
        Assert.AreEqual(2, allmoves.Length);

        // Cannot en passant when target is occupied
        piecePositions.Add(target, (Team.White, Piece.Queen));
        allmoves = MoveGenerator.GetAllPossiblePawnMoves(attackerLoc, Team.Black, state).ToArray();
        Assert.AreEqual((target, MoveType.Attack), allmoves[0]);
        Assert.AreEqual((attackerLoc.GetNeighborAt(HexNeighborDirection.Down).Value, MoveType.Move), allmoves[1]);
        Assert.AreEqual(2, allmoves.Length);
    }
}
