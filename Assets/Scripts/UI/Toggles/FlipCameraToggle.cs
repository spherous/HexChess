using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class FlipCameraToggle : MonoBehaviour
{
    public Toggle toggle;
    [SerializeField] private Image image;

    private void Awake()
    {
        toggle.isOn = PlayerPrefs.GetInt("AutoFlipCam", 1).IntToBool();
        toggle.onValueChanged.AddListener(newVal => 
            PlayerPrefs.SetInt("AutoFlipCam", newVal.BoolToInt())
        );
    }
}