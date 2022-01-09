using UnityEngine;

public class HostButton : TwigglyButton
{
    [SerializeField] private Networker networker;
    [SerializeField] private ModeText modeText;
    private new void Awake()
    {
        base.Awake();
        base.onClick += Clicked;
    }
    public void Clicked()
    {
        modeText.Show("Host a Private Match");
        networker.Host();
    } 
}