using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class FlipCameraToggle : MonoBehaviour
{
    public Toggle toggle;
    [SerializeField] private Image image;
    public Color uncheckedColor;
    public Color readyColor;

    private void Awake()
    {
        toggle.isOn = PlayerPrefs.GetInt("AutoFlipCam", 1).IntToBool();
        image.color = toggle.isOn ? readyColor : uncheckedColor;

        toggle.onValueChanged.AddListener(newVal => {
            PlayerPrefs.SetInt("AutoFlipCam", newVal.BoolToInt());
            image.color = newVal ? readyColor : uncheckedColor;
        });
    }
}