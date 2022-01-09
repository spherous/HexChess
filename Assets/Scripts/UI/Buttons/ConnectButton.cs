using UnityEngine;
using System.Net;
using System.Linq;
using Extensions;
using System;
using TMPro;

public class ConnectButton : TwigglyButton
{
    [SerializeField] private Networker networker;
    [SerializeField] private IPTextBox ipInput;
    [SerializeField] private TextMeshProUGUI ipErrorStates;
    [SerializeField] private ModeText modeText;
    public string errorTextToSet;
    private new void Awake()
    {
        base.Awake();
        base.onClick += Clicked;
    } 
    private void Update() {
        if(ipErrorStates != null && ipErrorStates.text != errorTextToSet)
        {
            ipErrorStates.text = errorTextToSet;
        }
    }
    public void Clicked()
    {
        if(!networker.attemptingConnection)
        {
            if(string.IsNullOrEmpty(ipInput.IP))
            {
                errorTextToSet = "IP field must not be empty.";
                ipErrorStates.text = errorTextToSet;
                return;
            }
            
            bool isDNS = false;
            try{
                isDNS = ipInput.IP.IsDNS();
                IPAddress addy = isDNS ? Dns.GetHostAddresses(ipInput.IP).First() : IPAddress.Parse(ipInput.IP);
            }
            catch(Exception e)
            {
                // parse this and write it to a error text for player
                ipErrorStates.text = e.Message;
                return;
            }

            // This very well may be a TextMeshProUGUI bug, any time the .text field is changed on a TextMeshProUGUI object, it's supposed to redraw the UI to be accurate.
            // But for some reason setting ipErrorStates.text to error in this callback action changes the text in the inspector but not in the game, thus inaccurate
            // We can work around by setting error to some cached string and checking if an update to the text is needed in the Update call.
            networker.TryConnectClient(ipInput.IP, networker.port, isDNS, error => {
                modeText.Show("Join a Private Match");
                if(!string.IsNullOrEmpty(error))
                    errorTextToSet = error;
            });
        }
    }
}