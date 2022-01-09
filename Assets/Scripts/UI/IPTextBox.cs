using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using static Extensions.StandaloneRulesExtensions;

public class IPTextBox : MonoBehaviour
{
    [SerializeField] private TMP_InputField ipInputField;
    public TMP_InputField inputField => ipInputField;
    public string IP {get; private set;}

    private void Awake() {
        ipInputField.onValueChanged.AddListener(newIP => {
            if(!newIP.Contains("*"))
                IP = newIP;
        });
        ipInputField.onEndEdit.AddListener(newIP => {
            IP = newIP;
            ipInputField.text = Regex.Replace(IP, IPHidingRegexMatchingPattern, "*");
        });
    }
    
    public void SetIP(string newIP)
    {
        IP = newIP;
        ipInputField.text = Regex.Replace(IP, IPHidingRegexMatchingPattern, "*");
    }
}