using UnityEngine;
using Extensions;

public class NotationFormToggle : MonoBehaviour
{
    public delegate void OnValueChanged(bool isShortForm);
    public OnValueChanged onValueChanged;
    [SerializeField] private CustomToggle longForm;
    [SerializeField] private CustomToggle shortForm;
    
    private void Awake() {
        bool prefIsShortForm = PlayerPrefs.GetInt("NotationType", 0).IntToBool();        
        shortForm.Toggle(prefIsShortForm);
        longForm.Toggle(!prefIsShortForm);

        longForm.onValueChanged += ToggleLong;
        shortForm.onValueChanged += ToggleShort;
    }

    public void ToggleLong(bool newVal) => shortForm.Toggle(!newVal);
    public void ToggleShort(bool newVal)
    {
        PlayerPrefs.SetInt("NotationType", newVal.BoolToInt());
        onValueChanged?.Invoke(newVal);
        longForm.Toggle(!newVal);
    }
}