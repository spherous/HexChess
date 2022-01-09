using UnityEngine;

public class HostObjectButton : MonoBehaviour, IObjectButton
{
    [SerializeField] private Networker networker;
    public void Click() => networker.Host();

    public void HoverEnter(){}
    public void HoverExit(){}
}