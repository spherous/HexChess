using System.Collections.Generic;
using System.Linq;
using Extensions;

[System.Serializable]
public struct Move
{
    public int turn;
    public Team lastTeam;
    public Piece lastPiece;
    public Index from;
    public Index to;
    public Piece? capturedPiece;
    public Piece? defendedPiece;
    public float duration;

    public Move(int turn, Team lastTeam, Piece lastPiece, Index from, Index to, Piece? capturedPiece = null, Piece? defendedPiece = null, float duration = 0)
    {
        this.turn = turn;
        this.lastTeam = lastTeam;
        this.lastPiece = lastPiece;
        this.from = from;
        this.to = to;
        this.capturedPiece = capturedPiece;
        this.defendedPiece = defendedPiece;
        this.duration = duration;
    }

    public override string ToString()
    {
        string capturedText = capturedPiece.HasValue ? $" Captured: {capturedPiece.Value}" : "";
        string defendedText = defendedPiece.HasValue ? $" Defeneded: {defendedPiece.Value}" : "";
        return $"On turn {turn} {lastTeam} took {duration} seconds to play {lastPiece} {from}->{to}{capturedText}{defendedText}";
    }
}