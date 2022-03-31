using UnityEngine;
using UnityEngine.UI;

public class FreePlaceModeToggle : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private GroupFader borderFader;
    [SerializeField] private GroupFader kingsOnlyFader;
    public Toggle toggle;

    private void Awake() => toggle.onValueChanged.AddListener(newVal => {
        if(newVal)
        {
            borderFader.FadeIn();
            kingsOnlyFader.FadeIn();
            return;
        }

        borderFader.FadeOut();
        kingsOnlyFader.FadeOut();
    });

    public void Disable() => gameObject.SetActive(false);
}