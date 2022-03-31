using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class KingsOnlyButton : TwigglyButton
{
    [SerializeField] private Board board;

    private new void Awake() {
        base.Awake();

        onClick += () => {
            IEnumerable<KeyValuePair<(Team, Piece), IPiece>> toRemove = new Dictionary<(Team, Piece), IPiece>(board.activePieces).Where(kvp => kvp.Key.Item2 != Piece.King).OrderBy(kvp => kvp.Key.Item2);
            foreach(KeyValuePair<(Team, Piece), IPiece> piece in toRemove)
                board.Enprison(piece.Value);
        };
    }
}