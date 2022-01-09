using TMPro;
using UnityEngine;

public class ForcePositiveIntInputField : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    private void Awake() {
        inputField.onValueChanged.AddListener(str => {
            if(int.TryParse(str, out int val) && val < 0)
                inputField.text = $"{Mathf.Abs(val)}";
        });
    }
}