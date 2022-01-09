using System;
using System.Linq;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class HostOrJoinPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI ipErrorStates;
    [SerializeField] public IPTextBox ipInput;
    [SerializeField] private Button closeButton;
    [SerializeField] private GroupFader fader;
    public bool visible => fader.visible;
    [SerializeField] private GameObject state1Container;
    [SerializeField] private GameObject state2Container;

    [SerializeField] private HostButton hostButton;
    [SerializeField] private JoinButton joinButton;
    [SerializeField] private ConnectButton connectButton;
    private void Awake() {
        closeButton.onClick.AddListener(() => Hide());
    }

    private void ReturnToDefault()
    {
        connectButton.errorTextToSet = "";
        ipErrorStates.text = "";
        titleText.text = "SELECT";
        state2Container.SetActive(false);
        state1Container.SetActive(true);
        hostButton.SetNorm();
        joinButton.SetNorm();
    }

    public void Show()
    {
        ReturnToDefault();
        fader.FadeIn();
    }

    public void Hide()
    {
        fader.FadeOut();
        ipInput.SetIP("");

        // Reset all twigglybuttons to norm
        var allTwigglyButtons = Resources.FindObjectsOfTypeAll<TwigglyButton>().ToList();
        foreach(var tb in allTwigglyButtons)
            tb.SetNorm();
    }

    public void EscapedInput(CallbackContext context)
    {
        if(!context.performed || !fader.visible)
            return;
        
        Hide();
    }

    public void EnterInput(CallbackContext context)
    {
        if(!context.performed || !state2Container.activeSelf)
            return;

        if(EventSystem.current.currentSelectedGameObject == ipInput.inputField.gameObject)
        {
            EventSystem.current.Deselect();
            connectButton.Clicked();
        }
    }

}