using Sirenix.OdinInspector;
using UnityEngine;
using Extensions;

public class GroupFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;
    public CanvasGroup group => _group;
    [SerializeField] private float fadeDuration;
    private float? startFadeTime;
    public TriSign sign {get; private set;} = TriSign.Zero;
    private bool deactivate = false;

    public bool visible => !(_group != null && _group.alpha < 1);
    public bool visibleOnStart = true;

    private void Awake()
    {
        if(_group == null)
            _group = gameObject.GetComponent<CanvasGroup>();

        _group.alpha = visibleOnStart ? 1 : 0;
        group.blocksRaycasts = visibleOnStart;
        group.interactable = visibleOnStart;
    }

    private void Update() {
        if(sign != TriSign.Zero && startFadeTime != null)
            FadeStep();
    }

    private void FadeStep()
    {
        _group.alpha = Mathf.Clamp01(_group.alpha + (sbyte)sign * (Time.unscaledDeltaTime / fadeDuration));
        
        if(Time.timeSinceLevelLoad >= startFadeTime.Value + fadeDuration)
        {
            sign = TriSign.Zero;
            startFadeTime = null;
            if(deactivate)
            {
                deactivate = false;
                gameObject.SetActive(false);
            }
        }
    }

    [Button]
    public void FadeOut(bool deactivateOnComplete = false)
    {
        if(group == null)
            return;
        // Debug.Log($"Closing {gameObject.name}.");
        group.alpha = 1;
        sign = TriSign.Negative;
        group.blocksRaycasts = false;
        group.interactable = false;
        startFadeTime = Time.timeSinceLevelLoad;
        if(deactivateOnComplete)
            deactivate = true;
    }

    [Button]
    public void FadeIn()
    {
        // Debug.Log($"Opening {gameObject.name}.");
        group.alpha = 0;
        sign = TriSign.Positive;
        startFadeTime = Time.timeSinceLevelLoad;
        group.blocksRaycasts = true;
        group.interactable = true;
    }

    public void Disable()
    {
        group.alpha = 0;
        sign = TriSign.Zero;
        startFadeTime = null;
        group.blocksRaycasts = false;
        group.interactable = false;
    }
}