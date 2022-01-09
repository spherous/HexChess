using TMPro;
using UnityEngine;

public class ConnectObjectButton : MonoBehaviour, IObjectButton
{
    [SerializeField] private Networker networker;
    [SerializeField] private TMP_InputField ipInput;
    public void Click()
    {
        if(!networker.attemptingConnection)
            networker.TryConnectClient(ipInput.text, networker.port);
    }
    public void HoverEnter(){}
    public void HoverExit(){}
}