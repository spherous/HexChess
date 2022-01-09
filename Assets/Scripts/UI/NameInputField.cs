using UnityEngine;
using TMPro;
using System.Text;

public class NameInputField : MonoBehaviour
{
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TextMeshProUGUI nameErrorStatesText;
    Networker networker;
    private void Awake()
    {
        networker = GameObject.FindObjectOfType<Networker>();
        input.text = PlayerPrefs.GetString("PlayerName", "GUEST");
        
        // should the error state text be on?
        nameErrorStatesText.gameObject.SetActive(string.IsNullOrEmpty(input.text) || input.text.ToUpper() == "GUEST");

        input.onValueChanged.AddListener(newVal => {
            // should the error state text be on?
            string newName = newVal.ToUpper();
            nameErrorStatesText.gameObject.SetActive(string.IsNullOrEmpty(newVal) || newName == "GUEST");

            // PlayerPrefs takes in a UTF8, but newName is UTF16, so it tries to convert it itself.
            // This is usually fine, except for when there is emotes in the string, causing a error to throw when failing to convert to UTF8.
            // If we do a conversion like this, the string should be safe for player prefs to use.
            string safeName = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(newName));

            PlayerPrefs.SetString("PlayerName", safeName);
            networker.UpdateName(newName);
        });
    }
}