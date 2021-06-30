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

    public string GetNotation(List<Promotion> promotions, BoardState boardState, NotationType notationType = NotationType.LongForm)
    {
        if(lastTeam == Team.None)
            return "";

        string fromIndex = from.GetKey();
        string toIndex = to.GetKey();
        string piece = GetStringForPiece(lastPiece, lastTeam, promotions);
        string type = capturedPiece.HasValue ? "x" : defendedPiece.HasValue ? "d" : "m";
        
        string otherPiece = type switch{
            "x" => GetStringForPiece(capturedPiece.Value, lastTeam == Team.White ? Team.Black : Team.White, promotions),
            "d" => GetStringForPiece(defendedPiece.Value, lastTeam, promotions),
            _ => ""
        };

        string promo = "";
        Team lt = lastTeam;
        Piece lp = lastPiece;

        IEnumerable<Promotion> applicablePromotions = promotions.Where(promo => promo.team == lt && promo.from == lp);
        if(applicablePromotions.Any())
        {
            Promotion applicablePromo = applicablePromotions.First();
            promo = applicablePromo.turnNumber == turn && lastTeam == applicablePromo.team ? $"={applicablePromo.to.GetPieceShortString()}" : "";
        }

        string check = "";
        if(boardState.checkmate != Team.None)
            check = "#";
        else if(boardState.check != Team.None)
            check = "+";

        // modify for shortform
        if(notationType == NotationType.ShortForm)
        {
            if(piece == "p")
            {
                piece = "";
                fromIndex = "";
                type = type == "m" ? "" : type;
            }
        }

        return $"{piece}{fromIndex}{type}{otherPiece}{toIndex}{promo}{check}";
    }

    private string GetStringForPiece(Piece potentialPawn, Team team, List<Promotion> promotions)
    {
        if(potentialPawn < Piece.Pawn1)
            return potentialPawn.GetPieceShortString();

        // The piece may habe been promoted. If so, we want to return the promoted piece. But only if it's not the turn the promo happened on   
        IEnumerable<Promotion> applicablePromotions = promotions.Where(promo => promo.team == team && promo.from == potentialPawn);
        if(applicablePromotions.Any())
        {
            Promotion applicablePromo = applicablePromotions.First();
            string result = applicablePromo.turnNumber < turn || lastTeam != applicablePromo.team ? applicablePromo.to.GetPieceShortString() : potentialPawn.GetPieceShortString();
            return $"{result}";
        }
        
        return potentialPawn.GetPieceShortString();
    }
}

public enum NotationType:int {LongForm = 0, ShortForm = 1}