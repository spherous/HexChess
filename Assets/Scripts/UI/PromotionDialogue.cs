using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PromotionDialogue : MonoBehaviour
{
    [SerializeField] private Button rookButton;
    [SerializeField] private Button knightButton;
    [SerializeField] private Button squireButton;
    [SerializeField] private Button queenButton;
    [SerializeField] private Button bishopButton;
    IEnumerable<(Button, Piece)> GetButtons()
    {
        yield return (rookButton, Piece.QueensRook);
        yield return (knightButton, Piece.QueensKnight);
        yield return (squireButton, Piece.BlackSquire);
        yield return (bishopButton, Piece.QueensBishop);
        yield return (queenButton, Piece.Queen);
    }

    private void Awake() => gameObject.SetActive(false);

    public void Display(Action<Piece> callback)
    {
        gameObject.SetActive(true);
        
        foreach((Button button, Piece piece) in GetButtons())
        {
            button.onClick.RemoveAllListeners();
            
            button.onClick.AddListener(() => {
                callback.Invoke(piece);
                gameObject.SetActive(false);
            });
        }
    }
}   