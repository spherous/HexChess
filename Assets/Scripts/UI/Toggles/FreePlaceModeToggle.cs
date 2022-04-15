using System;
using UnityEngine;
using UnityEngine.UI;

public class FreePlaceModeToggle : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private GroupFader borderFader;
    [SerializeField] private GroupFader kingsOnlyFader;
    [SerializeField] private GroupFader freePlaceFader;
    [SerializeField] private Board board;
    public Toggle toggle;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(newVal => {
            if(newVal)
            {
                borderFader.FadeIn();
                kingsOnlyFader.FadeIn();
                return;
            }

            borderFader.FadeOut();
            kingsOnlyFader.FadeOut();
        });

        board.gameOver += GameOver;
        board.newTurn += NewTurn;
    }

    private void NewTurn(BoardState newState)
    {
        if(!freePlaceFader.visible)
        {
            if(!freePlaceFader.gameObject.activeSelf)
                freePlaceFader.gameObject.SetActive(true);
            if(!borderFader.gameObject.activeSelf)
                borderFader.gameObject.SetActive(true);
            if(!kingsOnlyFader.gameObject.activeSelf)
                kingsOnlyFader.gameObject.SetActive(true);
            freePlaceFader.FadeIn();
        }
    }

    private void GameOver(Game game)
    {
        if(freePlaceFader.visible)
            freePlaceFader.FadeOut(true);
        if(kingsOnlyFader.visible)
            kingsOnlyFader.FadeOut(true);
        if(borderFader.visible)
            borderFader.FadeOut(true);
    }

    public void Disable()
    {
        if(freePlaceFader.gameObject.activeSelf)
            freePlaceFader.gameObject.SetActive(false);
        if(borderFader.gameObject.activeSelf)
            borderFader.gameObject.SetActive(false);
        if(kingsOnlyFader.gameObject.activeSelf)
            kingsOnlyFader.gameObject.SetActive(false);
    } 
}