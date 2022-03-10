using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaveScenePanel : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private GroupFader fader;

    private void Awake() {
        board.gameOver += GameOver;
    }

    private void GameOver(Game game)
    {
        fader?.FadeIn();
    }
}