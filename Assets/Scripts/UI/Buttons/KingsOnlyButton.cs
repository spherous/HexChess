using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class KingsOnlyButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Board board;

    private void Awake() {
        gameObject.SetActive(false);

        button.onClick.AddListener(() => {
            IEnumerable<KeyValuePair<(Team, Piece), IPiece>> toRemove = new Dictionary<(Team, Piece), IPiece>(board.activePieces).Where(kvp => kvp.Key.Item2 != Piece.King).OrderBy(kvp => kvp.Key.Item2);
            foreach(KeyValuePair<(Team, Piece), IPiece> piece in toRemove)
                board.Enprison(piece.Value);
        });
    }
}
