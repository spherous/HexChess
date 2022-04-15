using UnityEngine;

public class JumpForwardTenButton : TwigglyButton
{
    [SerializeField] private TurnHistoryPanel historyPanel;
    private new void Awake()
    {
        base.Awake();
        onClick += () => historyPanel.JumpForwardTen();
    }
}