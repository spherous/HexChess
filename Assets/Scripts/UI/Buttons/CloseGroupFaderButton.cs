using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseGroupFaderButton : TwigglyButton
{
    [SerializeField] private GroupFader groupFader;
    private new void Awake() {
        base.Awake();
        onClick += () => {
            if(groupFader.visible)
                groupFader.FadeOut();
        };
    }
}
