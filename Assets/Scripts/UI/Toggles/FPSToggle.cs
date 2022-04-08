using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class FPSToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    FPS fps;

    private void Start() {
        fps = GameObject.FindObjectOfType<FPS>();

        if(PlayerPrefs.GetInt("ShowFPS", false.BoolToInt()).IntToBool())
        {
            toggle.isOn = true;
            fps?.Toggle(toggle.isOn);
        }

        toggle.onValueChanged.AddListener(isOn => {
            if(fps == null)
                fps = GameObject.FindObjectOfType<FPS>();
            fps?.Toggle(isOn);
        });
    }
}