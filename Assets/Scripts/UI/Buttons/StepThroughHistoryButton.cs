using UnityEngine;

public class StepThroughHistoryButton : TwigglyButton
{
    [SerializeField] private TurnHistoryPanel historyPanel;
    public int dir;
    private new void Awake()
    {
        base.Awake();
        onClick += () => historyPanel.HistoryStep(dir);
    }
}