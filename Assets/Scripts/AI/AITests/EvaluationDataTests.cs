using NUnit.Framework;

public class EvaluationDataTests
{
    [Test]
    public void PiecesCountTest()
    {
        var board = new FastBoardNode();
        var evalData = new EvaluationData();

        evalData.Prepare(board);
        Assert.That(evalData.WhitePieces.Count, Is.EqualTo(0));
        Assert.That(evalData.BlackPieces.Count, Is.EqualTo(0));

        board[new Index(1, 'A')] = (Team.White, FastPiece.King);
        evalData.Prepare(board);
        Assert.That(evalData.WhitePieces.Count, Is.EqualTo(1));
        Assert.That(evalData.BlackPieces.Count, Is.EqualTo(0));

        board[new Index(9, 'A')] = (Team.Black, FastPiece.King);
        evalData.Prepare(board);
        Assert.That(evalData.WhitePieces.Count, Is.EqualTo(1));
        Assert.That(evalData.BlackPieces.Count, Is.EqualTo(1));
    }

    [Test]
    public void ThreatCountTest()
    {
        var board = new FastBoardNode();
        var evalData = new EvaluationData();

        board[new Index(5, 'E')] = (Team.White, FastPiece.King);
        evalData.Prepare(board);
        Assert.That(evalData.WhiteThreats.Count, Is.EqualTo(6));
        Assert.That(evalData.BlackThreats.Count, Is.EqualTo(0));

        board[new Index(5, 'E')] = (Team.White, FastPiece.Knight);
        evalData.Prepare(board);
        Assert.That(evalData.WhiteThreats.Count, Is.EqualTo(12));
        Assert.That(evalData.BlackThreats.Count, Is.EqualTo(0));

        board[new Index(5, 'E')] = (Team.Black, FastPiece.Squire);
        evalData.Prepare(board);
        Assert.That(evalData.WhiteThreats.Count, Is.EqualTo(0));
        Assert.That(evalData.BlackThreats.Count, Is.EqualTo(6));

        board[new Index(5, 'E')] = (Team.Black, FastPiece.Pawn);
        evalData.Prepare(board);
        Assert.That(evalData.WhiteThreats.Count, Is.EqualTo(0));
        Assert.That(evalData.BlackThreats.Count, Is.EqualTo(2));

        board[new Index(5, 'E')] = (Team.None, FastPiece.Pawn);
        board[new Index(1, 'D')] = (Team.Black, FastPiece.Pawn);
        evalData.Prepare(board);
        Assert.That(evalData.WhiteThreats.Count, Is.EqualTo(0));
        Assert.That(evalData.BlackThreats.Count, Is.EqualTo(0));
    }
}
