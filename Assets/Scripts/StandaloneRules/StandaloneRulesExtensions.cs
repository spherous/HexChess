using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Extensions
{
    public static class StandaloneRulesExtensions 
    {
        public static int FloorToInt(this float val) => (int)Math.Floor(val);
        public static int BoolToInt(this bool val) => val ? 1 : 0;
        public static bool IntToBool(this int val) => val == 0 ? false : true;
        public static HexNeighborDirection OppositeDirection(this HexNeighborDirection direction) =>
            (HexNeighborDirection)(((int)direction + 3) % 6);

        // White is an enemy to black, but none has no enemy, so we return none as the enemy to none
        public static Team Enemy(this Team team) => team == Team.None ? Team.None : team == Team.White ? Team.Black : Team.White;

        public static bool IsRook(this Piece piece) => piece == Piece.KingsRook || piece == Piece.QueensRook;
        public static bool IsKnight(this Piece piece) => piece == Piece.KingsKnight || piece == Piece.QueensKnight;
        public static bool IsBishop(this Piece piece) => piece == Piece.KingsBishop || piece == Piece.QueensBishop;
        public static bool IsSquire(this Piece piece) => piece == Piece.WhiteSquire || piece == Piece.GraySquire || piece == Piece.BlackSquire;
        public static bool IsPawn(this Piece piece) => piece >= Piece.Pawn1;

        public static string GetPieceShortString(this Piece piece) => piece switch{
            Piece.King => "k", 
            Piece.Queen => "q",
            Piece p when p.IsRook() => "r",
            Piece p when p.IsKnight() => "n",
            Piece p when p.IsBishop() => "b",
            Piece p when p.IsSquire() => "s",
            Piece p when p.IsPawn() => "p",
            _ => ""
        };

        public static string GetPieceLongString(this Piece piece) => piece switch {
            Piece.King => "King",
            Piece.Queen => "Queen",
            Piece p when p.IsRook() => "Rook",
            Piece p when p.IsKnight() => "Knight",
            Piece p when p.IsBishop() => "Bishop",
            Piece p when p.IsSquire() => "Squire",
            Piece p when p.IsPawn() => "Pawn",
            _ => ""
        };


        public static int GetMaterialValue(this Piece piece) => piece switch {
            Piece.Queen => 9,
            Piece p when p.IsRook() => 5,
            Piece p when p.IsBishop() || p.IsKnight() => 3,
            Piece p when p.IsSquire() => 2,
            Piece p when p.IsPawn() => 1,
            _ => 0
        };

        public static int GetMaxMoveCount(this Piece piece) => piece switch {
            Piece.King => 6,
            Piece.Queen => 58,
            Piece p when p.IsRook() => 30,
            Piece p when p.IsKnight() => 12,
            Piece p when p.IsBishop() => 32,
            Piece p when p.IsSquire() => 6,
            Piece p when p.IsPawn() => 4,
            _ => 0
        };
        
        public static Piece[] GetAlternates(this Piece piece) => piece switch{
            Piece.BlackSquire => new Piece[2]{Piece.WhiteSquire, Piece.GraySquire},
            Piece.WhiteSquire => new Piece[2]{Piece.BlackSquire, Piece.GraySquire},
            Piece.GraySquire => new Piece[2]{Piece.BlackSquire, Piece.WhiteSquire},
            Piece.KingsBishop => new Piece[1]{Piece.QueensBishop},
            Piece.QueensBishop => new Piece[1]{Piece.KingsBishop},
            Piece.KingsRook => new Piece[1]{Piece.QueensRook},
            Piece.QueensRook => new Piece[1]{Piece.KingsRook},
            Piece.KingsKnight => new Piece[1]{Piece.QueensKnight},
            Piece.QueensKnight => new Piece[1]{Piece.KingsKnight},
            _ => Array.Empty<Piece>()
        };
        
        public static Team GetCurrentTurn(this List<BoardState> turnHistory) => turnHistory[turnHistory.Count - 1].currentMove;

        public static string IP(this TcpClient client) => $"{((IPEndPoint)client?.Client.RemoteEndPoint).Address}";
        public static bool IsDNS(this string input) => Uri.CheckHostName(input) == UriHostNameType.Dns;
        public static string IPHidingRegexMatchingPattern => "[a-zA-Z0-9]";
    }
}
