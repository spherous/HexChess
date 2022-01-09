using TMPro;
using UnityEngine;

public class ModeText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI modeText;
    [SerializeField] private GroupFader fader;

    public void Show(string text)
    {
        modeText.text = text;
        fader.FadeIn();
    }
}