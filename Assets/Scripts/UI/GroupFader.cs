using Sirenix.OdinInspector;
using UnityEngine;
using Extensions;

[RequireComponent(typeof(CanvasGroup))]
public class GroupFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;
    public CanvasGroup group => _group;
    [SerializeField] private float fadeDuration;
    private float? startFadeTime;
    public TriSign sign {get; private set;} = TriSign.Zero;
    private bool deactivate = false;

    public bool visible => isCompletelyVisibileOrFadingIn && !isCompletelyInvisibleOrFadingOut;
    private bool isFadingIn => group.alpha < 1 && sign == TriSign.Positive;
    private bool isFadingOut => group.alpha > 0 && sign == TriSign.Negative;
    public bool isCompletelyVisibileOrFadingIn => group != null && (isFadingIn || (group.alpha >= 0.98f && sign == TriSign.Zero));
    public bool isCompletelyInvisibleOrFadingOut => group != null && (isFadingOut || (group.alpha <= 0.02f && sign == TriSign.Zero));
    public bool visibleOnStart = true;

    private void Awake()
    {
        if(group == null)
            _group = gameObject.GetComponent<CanvasGroup>();

        group.alpha = visibleOnStart ? 1 : 0;
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
        
        // If clicked too quickly (within the epsilon), the fader may think it has more work to do than it really does. 
        // We can prevent that extra work when the player clicks too quickly by checking if it's happening and stopping it.
        if(_group != null && ((sign == TriSign.Positive && _group.alpha >= 0.98f) || (sign == TriSign.Negative && _group.alpha <= 0.02f)))
        {
            _group.alpha = sign == TriSign.Positive ? 1 : 0;
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
        // group.alpha = 1;
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
        // group.alpha = 0;
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