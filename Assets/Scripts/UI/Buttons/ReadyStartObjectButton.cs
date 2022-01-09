using System;
using System.Collections.Generic;
using Extensions;
using TMPro;
using UnityEngine;

public class ReadyStartObjectButton : MonoBehaviour, IObjectButton
{
    public enum Mode{None = 0, NotReady = 1, Ready = 2, Start = 3}

    public List<MeshRenderer> hexesToOutline = new List<MeshRenderer>();

    [SerializeField] private Lobby lobby;
    [SerializeField] private LiftOnHover liftOnHover;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private TextMeshPro buttonText;

    public Material stardardHexMat;
    public Material buttonHexMat;

    private MaterialPropertyBlock propertyBlock;
    
    public Mode mode {get; private set;} = Mode.None;

    bool hovered = false;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        SetMode(Mode.None);
    }

    public void SetMode(Mode mode)
    {
        Mode lastMode = this.mode;
        this.mode = mode;

        if(this.mode == Mode.NotReady || this.mode == Mode.Ready || this.mode == Mode.Start)
            SetEnabledState(lastMode, mode.GetText());
        else
            SetDisabledState(lastMode);
    }

    private void SetDisabledState(Mode lastMode)
    {
        if(liftOnHover != null)
            liftOnHover.LockDown();
        if(meshRenderer != null)
        {
            meshRenderer.material = stardardHexMat;

            propertyBlock.SetFloat("_HighlightEdges", false.BoolToInt());
            meshRenderer.SetPropertyBlock(propertyBlock);
            hexesToOutline.ForEach(mr => mr.SetPropertyBlock(propertyBlock));
        }
        if(buttonText != null)
            buttonText.text = "";
    }

    private void SetEnabledState(Mode lastMode, string text)
    {
        if(liftOnHover != null && liftOnHover.lockedDown)
        {
            liftOnHover.Unlock();
            if(!hovered && liftOnHover.isUp)
                liftOnHover.Reset();
        }
        if(meshRenderer != null && lastMode == Mode.None)
            meshRenderer.material = buttonHexMat;
        if(buttonText != null)
            buttonText.text = hovered ? GetHoveredText(mode) : text;
    }

    public void Click()
    {
        Action action = mode switch {
            Mode.NotReady => () => ToggleReady(true), // move to Ready state
            Mode.Ready => () => ToggleReady(false), // move to NotReady state
            Mode.Start => StartMatch, 
            _ => null
        };

        action?.Invoke();
    }

    public void ToggleReady(bool newVal, bool forceReset = false)
    {
        Networker networker = GameObject.FindObjectOfType<Networker>();
        MessageType readyMessageType = newVal ? MessageType.Ready : MessageType.Unready;
        networker?.SendMessage(new Message(readyMessageType));
        SetMode(newVal ? Mode.Ready : Mode.NotReady);
        lobby?.ReadyLocal(newVal);
        
        propertyBlock.SetFloat("_HighlightEdges", newVal.BoolToInt());
        meshRenderer.SetPropertyBlock(propertyBlock);
        hexesToOutline.ForEach(mr => mr.SetPropertyBlock(propertyBlock));

        if(newVal)
            liftOnHover.LockUp();
        else
        {
            liftOnHover.Unlock();
            if(forceReset && liftOnHover.isUp)
                liftOnHover.Reset();
        }
    }

    private void StartMatch()
    {
        // If multiplayer game and opponent is readied, start game
        Networker networker = GameObject.FindObjectOfType<Networker>();
        if(networker != null && networker.clientIsReady && networker.isHost)
            networker.HostMatch();
        // If networker is null, we should load into an AI match with whatever AI settings the player set
        else if(networker == null)
            lobby.LoadAIGame();
    }

    public void HoverEnter()
    {
        hovered = true;
        buttonText.text = GetHoveredText(mode);
    }

    private string GetHoveredText(Mode modeToGet) => modeToGet switch{
        Mode.Ready => "Click to Cancel",
        Mode.NotReady => "Click to Ready",
        Mode.Start => Mode.Start.GetText(),
        _ => ""
    };

    public void HoverExit()
    {
        hovered = false;
        
        if(mode == Mode.Ready || mode == Mode.NotReady)
            SetEnabledState(mode, mode.GetText());
    }
}