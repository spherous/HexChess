using UnityEngine;
using TMPro;

public class Latency : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI latencyText;

    public void UpdateLatency(int latencyMs) => latencyText.text = $"{latencyMs} ms";
}