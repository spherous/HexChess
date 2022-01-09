using UnityEngine;
using UnityEngine.UI;

public class FreePlaceModeToggle : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Image screenBorderImage;
    [SerializeField] private Button kingsOnlyButton;
    public Toggle toggle;

    public Color uncheckedColor;
    public Color readyColor;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(newVal => {
            image.color = newVal ? readyColor : uncheckedColor;

            screenBorderImage.enabled = newVal;
            kingsOnlyButton.gameObject.SetActive(newVal);
        });
    }

    public void Disable() => gameObject.SetActive(false);
}
