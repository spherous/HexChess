using System.Collections.Generic;
using System.Linq;
using Extensions;

public static class Notation
{
    public const string defendNotation = "-";
    public const string swapNotation = "~";
    public const string moveNotation = "m";
    public const string captureNotation = "x";
    public const string checkNotation = "+";
    public const string checkmateNotation = "#";
    public static string Get(BoardState boardState, Move move, List<Promotion> promotions, NotationType notationType = NotationType.LongForm)
    {
        if(move.lastTeam == Team.None)
            return "";

        string fromIndex = move.from.GetKey();
        string toIndex = move.to.GetKey();
        string piece = GetStringForPiece(move.lastPiece, move.lastTeam, promotions, move);
        string type = move.capturedPiece.HasValue ? captureNotation : move.defendedPiece.HasValue ? defendNotation : moveNotation;
        
        // When using freeplace mode, you can swap non-rook pieces, we want to clearly label that as a swap and not a defend unless a rook is involved
        if(type == defendNotation && !HexachessagonEngine.GetRealPiece((move.lastTeam, move.lastPiece), promotions).IsRook())
            type = swapNotation;

        string otherPiece = type switch{
            captureNotation => GetStringForPiece(move.capturedPiece.Value, move.lastTeam.Enemy(), promotions, move),
            string t when (t == defendNotation || t == swapNotation) => GetStringForPiece(move.defendedPiece.Value, move.lastTeam, promotions, move),
            _ => ""
        };

        string freePlaced = "";

        bool shortFormMod = notationType == NotationType.ShortForm;

        if(move.from == Index.invalid && move.to == Index.invalid)
        {   
            // No piece moved - skipped move with free place mode
            fromIndex = "";
            toIndex = "";
            piece = "";
            type = "";
            otherPiece = "";
            freePlaced = "skipped";
            shortFormMod = false;
        }
        else if(move.to == Index.invalid)
        {
            // Put in jail with free place mode
            otherPiece = "";
            type = "";
            toIndex = "";
            freePlaced = " jailed";
            shortFormMod = false;
        }
        else if(move.from == Index.invalid)
        {
            // Freed from jail with free place mode
            fromIndex = "";
            otherPiece = "";
            type = "";
            freePlaced = " freed to ";
            shortFormMod = false;
        }
        
        string promo = "";
        Team lt = move.lastTeam;
        Piece lp = move.lastPiece;

        IEnumerable<Promotion> applicablePromotions = promotions.Where(promo => promo.team == lt && promo.from == lp);
        if(applicablePromotions.Any())
        {
            Promotion applicablePromo = applicablePromotions.First();
            promo = applicablePromo.turnNumber == move.turn + (move.lastTeam == Team.Black).BoolToInt() && move.lastTeam == applicablePromo.team 
                ? $"={applicablePromo.to.GetPieceShortString()}" 
                : "";
        }
        
        string check = boardState.checkmate != Team.None ? checkmateNotation : boardState.check != Team.None ? checkNotation : "";

        // modify for shortform
        if(notationType == NotationType.ShortForm && shortFormMod)
        {
            if(piece == "p")
            {
                piece = "";
                if(type != swapNotation)
                {
                    otherPiece = "";
                    type = type == captureNotation ? type : "";
                    fromIndex = type == captureNotation ? $"{fromIndex.First()}" : "";
                }
            }
            else
            {
                List<Piece> alternatePieces = HexachessagonEngine.GetRealPiece((lt, lp), promotions).GetAlternates().ToList();

                foreach(Promotion potentialAlternate in promotions)
                {
                    if(potentialAlternate.turnNumber < move.turn)
                    {
                        List<Piece?> toAdd = new List<Piece?>();
                        foreach(Piece alt in alternatePieces)
                        {
                            if(potentialAlternate.to == alt)
                                toAdd.Add(potentialAlternate.from);
                        }

                        toAdd.ForEach(p => alternatePieces.Add(p.Value));

                        if(potentialAlternate.from == lp)
                            alternatePieces.Add(potentialAlternate.to);
                        
                        if(potentialAlternate.to == lp)
                            alternatePieces.Add(potentialAlternate.from);
                    }
                }

                type = type == moveNotation ? "" : type;

                if(alternatePieces.Count > 0)
                {
                    bool fileAmbiguity = false;
                    bool rankAmbiguity = false;

                    foreach(Piece alternate in alternatePieces)
                    {
                        if(boardState.TryGetIndex((lt, alternate), out Index index))
                        {
                            Piece realPiece = HexachessagonEngine.GetRealPiece((lt, alternate), promotions);

                            var possibleMovesConcerningHex = MoveGenerator.GetAllPossibleMoves(index, realPiece, lt, boardState, promotions, true)
                                .Where(kvp => kvp.target != null && kvp.target == move.to);

                            if(MoveValidator.ValidateMoves(possibleMovesConcerningHex, (lt, alternate), boardState, promotions).Any())
                            {
                                if(index.col == move.from.col)
                                    rankAmbiguity = true;
                                if(index.row == move.from.row)
                                    fileAmbiguity = true;
                            }
                        }
                    }

                    if(fileAmbiguity && rankAmbiguity)
                        fromIndex = move.from.GetKey();
                    else if(fileAmbiguity)
                        fromIndex = $"{move.from.GetLetter()}";
                    else if(rankAmbiguity)
                        fromIndex = $"{move.from.GetNumber()}";
                    else
                        fromIndex = "";
                }
                else
                    fromIndex = "";
            }
        }

        return $"{piece}{fromIndex}{type}{otherPiece}{freePlaced}{toIndex}{promo}{check}";
    }
    
    private static string GetStringForPiece(Piece potentialPawn, Team team, List<Promotion> promotions, Move move)
    {
        if(!potentialPawn.IsPawn())
            return potentialPawn.GetPieceShortString();

        // The piece may habe been promoted. If so, we want to return the promoted piece. But only if it's not the turn the promo happened on   
        IEnumerable<Promotion> applicablePromotions = promotions.Where(promo => promo.team == team && promo.from == potentialPawn);
        if(applicablePromotions.Any())
        {
            Promotion applicablePromo = applicablePromotions.First();
            string result = applicablePromo.turnNumber < move.turn || move.lastTeam != applicablePromo.team 
                ? applicablePromo.to.GetPieceShortString() 
                : potentialPawn.GetPieceShortString();
            return $"{result}";
        }
        
        return potentialPawn.GetPieceShortString();
    }
}

public enum NotationType:int {LongForm = 0, ShortForm = 1}