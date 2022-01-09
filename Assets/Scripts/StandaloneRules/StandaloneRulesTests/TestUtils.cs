#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Extensions;

internal class TestUtils
{
    internal static BoardState CreateBoardState(IEnumerable<(Index location, Team team, Piece piece)>? pieces)
    {
        return CreateBoardState(Team.White, pieces);
    }
    internal static BoardState CreateBoardState(Team toMove, IEnumerable<(Index location, Team team, Piece piece)>? pieces)
    {
        var allPiecePositions = new BidirectionalDictionary<(Team, Piece), Index>();
        if (pieces != null)
        {
            foreach (var piece in pieces)
            {
                allPiecePositions.Add((piece.team, piece.piece), piece.location);
            }
        }

        return new BoardState(allPiecePositions, toMove, Team.None, Team.None, 0);
    }
}
