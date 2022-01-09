using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Extensions.StandaloneRulesExtensions;

public class IPPanel : MonoBehaviour
{
    [SerializeField] private Button copyIPButton;
    [SerializeField] private GroupFader copiedText;
    [SerializeField] private GroupFader ipFader;
    [SerializeField] private Toggle ipvisibleToggle;
    [SerializeField] private Toggle ipvisibleTextToggle;
    [SerializeField] private Image visibleIconImage;
    [SerializeField] private TextMeshProUGUI ipText;

    bool visible = false;

    public Sprite vibileIcon;
    public Sprite invisibleIcon;

    public float showCopiedDuration;
    private float hideCopiedAtTime;
    private string IP; 
    private void Awake() {
        IP = Networker.GetPublicIPAddress();
        HideIP();
        ipvisibleToggle.onValueChanged.AddListener(newVal => 
        {
            Toggle(newVal);
            ipvisibleTextToggle.isOn = newVal;
        });
        ipvisibleTextToggle.onValueChanged.AddListener(newVal =>
        {
            Toggle(newVal);
            ipvisibleToggle.isOn = newVal;
        });
        copyIPButton.onClick.AddListener(() => {
            copiedText.FadeIn();
            hideCopiedAtTime = Time.timeSinceLevelLoad + showCopiedDuration;
            GUIUtility.systemCopyBuffer = IP;
        });
    }

    private void Update() {
        if(copiedText.visible && Time.timeSinceLevelLoad >= hideCopiedAtTime)
            copiedText.FadeOut();
    }

    public void Toggle(bool newVal)
    {
        if(newVal && !visible)
            ShowIP();
        else if(!newVal && visible)
            HideIP();
    }

    [Button]
    public void HideIP()
    {
        // If the IP was unable to be fetched, we don't want to hide that information from the player
        if(IP == "Failed to fetch IP.")
            return;

        visible = false;
        visibleIconImage.sprite = invisibleIcon;
        ipText.text = Regex.Replace(IP, IPHidingRegexMatchingPattern, "*");
    }

    [Button]
    public void ShowIP()
    {
        visible = true;
        visibleIconImage.sprite = vibileIcon;
        ipText.text = IP;
    }

    public void SetIP(string newIP)
    {
        IP = newIP;
        HideIP();
    }

    public void FadeIn() => ipFader.FadeIn();
    public void FadeOut() => ipFader.FadeOut();
}