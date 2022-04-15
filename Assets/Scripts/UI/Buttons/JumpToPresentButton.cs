using UnityEngine;

public class JumpToPresentButton : TwigglyButton
{
    [SerializeField] private TurnHistoryPanel historyPanel;
    private new void Awake()
    {
        base.Awake();
        onClick += () => historyPanel.JumpToPresent();
    }
}