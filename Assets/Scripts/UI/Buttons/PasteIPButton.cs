using UnityEngine;
using UnityEngine.UI;

public class PasteIPButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private IPTextBox ipText;

    private void Awake() => button.onClick.AddListener(() => ipText.SetIP(GUIUtility.systemCopyBuffer));
}