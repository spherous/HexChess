using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class LatencyToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    private Latency latency;
    private void Awake() =>
        latency = GameObject.FindObjectOfType<Latency>();

    private void Start() {
        toggle.isOn = PlayerPrefs.GetInt("Latency", true.BoolToInt()).IntToBool();
        
        toggle.onValueChanged.AddListener(isOn => {
            PlayerPrefs.SetInt("Latency", isOn.BoolToInt());
            if(latency == null)
                latency = GameObject.FindObjectOfType<Latency>();
            latency?.Toggle(isOn);
        });
    }
}