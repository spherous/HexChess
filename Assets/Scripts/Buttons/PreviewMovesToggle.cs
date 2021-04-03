using UnityEngine;
using UnityEngine.UI;

public class PreviewMovesToggle : MonoBehaviour
{
    public Toggle toggle;
    [SerializeField] private Image image;
    Networker networker;
    public Color uncheckedColor;
    public Color readyColor;

    private void Awake() {
        networker = GameObject.FindObjectOfType<Networker>();

        toggle.onValueChanged.AddListener(newVal => {
            MessageType previewMovesType = newVal ? MessageType.PreviewMovesOn : MessageType.PreviewMovesOff;
            networker?.SendMessage(new Message(previewMovesType));
            image.color = newVal ? readyColor : uncheckedColor;
        });
        toggle.isOn = true;
    }
}