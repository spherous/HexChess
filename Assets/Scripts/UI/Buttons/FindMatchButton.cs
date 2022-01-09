using UnityEngine;

public class FindMatchButton : TwigglyButton
{
    [SerializeField] private Networker networker;
    [SerializeField] private ModeText modeText;
    [SerializeField] private IPPanel ipPanel;
    public string dns;
    private new void Awake()
    {
        base.Awake();
        base.onClick += Clicked;
    }
    public void Clicked()
    {
        modeText.Show("Quick Play");
        networker.TryConnectClient(dns, networker.port, true);
        ipPanel?.FadeOut();
    }
}