using TMPro;
using UnityEngine;

public class TurnChangePanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI whiteText;
    [SerializeField] private TextMeshProUGUI blackText;
    
    public float displayLength;
    private float closeAtTime;

    public float fadeDuration;
    private float fadeElapsed;
    
    private Fading fading;

    private void Awake() => canvasGroup.alpha = 0;

    public void Display(Team team)
    {
        if(team == Team.White)
        {
            blackText.gameObject.SetActive(false);
            whiteText.gameObject.SetActive(true);
        }
        else if(team == Team.Black)
        {
            blackText.gameObject.SetActive(true);
            whiteText.gameObject.SetActive(false);
        }
        
        fading = Fading.In;
        fadeElapsed = 0;
    }

    public void Hide()
    {
        fading = Fading.Out;
        fadeElapsed = 0;
    }

    private void Update()
    {
        if(fading > Fading.None)
        {
            fadeElapsed = Mathf.Clamp(fadeElapsed + Time.deltaTime, 0, fadeDuration);

            if(fading == Fading.In)
            {
                canvasGroup.alpha = Mathf.Clamp01(fadeElapsed / fadeDuration);
                if(canvasGroup.alpha == 1)
                    closeAtTime = displayLength + Time.timeSinceLevelLoad;
            }
            else if(fading == Fading.Out)
                canvasGroup.alpha = Mathf.Clamp01(1 - (fadeElapsed / fadeDuration));

            if(fadeElapsed == fadeDuration)
                fading = Fading.None;
        }
        else if(canvasGroup.alpha == 1)
        {
            if(Time.timeSinceLevelLoad >= closeAtTime)
                Hide();
        }
    }
}

public enum Fading {None, In, Out}