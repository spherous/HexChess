using System;
public enum FastPiece : byte
{
    None = 0,
    Pawn,
    Squire,
    Knight,
    Rook,
    Bishop,
    Queen,
    King,
}

namespace Extensions
{
    public static class FastPieceExtensions
    {
        public static FastPiece ToFastPiece(this Piece piece)
        {
            switch (piece)
            {
                case Piece.King:
                    return FastPiece.King;

                case Piece.Queen:
                    return FastPiece.Queen;

                case Piece.KingsRook:
                case Piece.QueensRook:
                    return FastPiece.Rook;

                case Piece.KingsKnight:
                case Piece.QueensKnight:
                    return FastPiece.Knight;

                case Piece.KingsBishop:
                case Piece.QueensBishop:
                    return FastPiece.Bishop;

                case Piece.WhiteSquire:
                case Piece.GraySquire:
                case Piece.BlackSquire:
                    return FastPiece.Squire;

                case Piece.Pawn1:
                case Piece.Pawn2:
                case Piece.Pawn3:
                case Piece.Pawn4:
                case Piece.Pawn5:
                case Piece.Pawn6:
                case Piece.Pawn7:
                case Piece.Pawn8:
                default:
                    return FastPiece.Pawn;
            }
        }

        public static Piece ToPiece(this FastPiece piece)
        {
            switch (piece)
            {
                case FastPiece.King:
                    return Piece.King;
                case FastPiece.Queen:
                    return Piece.Queen;
                case FastPiece.Rook:
                    return Piece.KingsRook;
                case FastPiece.Bishop:
                    return Piece.KingsBishop;
                case FastPiece.Knight:
                    return Piece.KingsKnight;
                case FastPiece.Squire:
                    return Piece.GraySquire;
                case FastPiece.Pawn:
                    return Piece.Pawn1;
                default:
                    return Piece.Pawn1;
            }
        }
    }
}
