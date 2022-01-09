using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrivateMatchButton : TwigglyButton
{
    [SerializeField] private HostOrJoinPanel hostOrJoinPanel;
    private new void Awake() {
        base.Awake();
        base.onClick += Clicked;
    }

    public void Clicked()
    {
        if(hostOrJoinPanel.visible)
            hostOrJoinPanel.Hide();
        else
            hostOrJoinPanel.Show();
    }
}