using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class OptionsPanel : MonoBehaviour
{
    private Keys keys;
    [SerializeField] private GroupFader fader;
    public bool visible => fader != null && fader.visible;
    private void Awake() => keys = FindObjectOfType<Keys>();
    public void Escaped(CallbackContext context)
    {
        if(!context.performed)
            return;
        
        if(fader.visible)
            fader.FadeOut();
        else
        {
            fader.FadeIn();
            keys.Clear();
        }
    }
}