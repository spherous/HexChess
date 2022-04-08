using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class OptionsPanel : MonoBehaviour
{
    [SerializeField] private GroupFader fader;
    public bool visible => fader != null && fader.visible;
    public void Escaped(CallbackContext context)
    {
        if(!context.performed)
            return;
        
        if(fader.visible)
            fader.FadeOut();
        else
            fader.FadeIn();
    }
}
