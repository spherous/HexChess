using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OfferDrawButton : MonoBehaviour
{
    [SerializeField] private Button button;
    Board board;

    private void Awake() {
        board = GameObject.FindObjectOfType<Board>();
        button.onClick.AddListener(() => {
            Networker networker = GameObject.FindObjectOfType<Networker>();
            if(networker != null)
                networker.SendMessage(new Message(MessageType.OfferDraw));
        });
        board.gameOver += GameOver;
    }

    private void GameOver(Game game)
    {
        board.gameOver -= GameOver;
        button.onClick.RemoveAllListeners();
        Destroy(gameObject);
    }
}
