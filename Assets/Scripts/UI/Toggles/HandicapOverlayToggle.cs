using Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandicapOverlayToggle : MonoBehaviour
{
    public Toggle toggle;
    Networker networker;

    private void Awake()
    {
        toggle.isOn = PlayerPrefs.GetInt("HandicapOverlay", 1).IntToBool();

        networker = GameObject.FindObjectOfType<Networker>();

        toggle.onValueChanged.AddListener(newVal =>
        {
            PlayerPrefs.SetInt("HandicapOverlay", newVal.BoolToInt());

            MessageType handicapOverlayType = newVal ? MessageType.HandicapOverlayOn : MessageType.HandicapOverlayOff;
            networker?.SendMessage(new Message(handicapOverlayType));
            EventSystem.current.Deselect();
        });
    }
}