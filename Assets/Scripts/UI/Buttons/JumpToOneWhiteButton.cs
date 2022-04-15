using UnityEngine;

public class JumpToOneWhiteButton : TwigglyButton
{
    [SerializeField] private TurnHistoryPanel historyPanel;
    private new void Awake()
    {
        base.Awake();
        onClick += () => historyPanel.JumpToFirst();
    }
}