using UnityEngine;

public class JumpBackTenButton : TwigglyButton
{
    [SerializeField] private TurnHistoryPanel historyPanel;
    private new void Awake()
    {
        base.Awake();
        onClick += () => historyPanel.JumpBackwardTen();
    }
}