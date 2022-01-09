using UnityEngine;

public class FindMatchObjectButton : MonoBehaviour, IObjectButton
{
    [SerializeField] private Networker networker;
    public string dns;

    public void Click() => networker.TryConnectClient(dns, networker.port, true);
    public void HoverEnter(){}
    public void HoverExit(){}
}