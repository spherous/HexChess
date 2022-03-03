using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class MenuCamera : MonoBehaviour
{
    [SerializeField] private Camera cam;
    private LiftOnHover lastHovered;
    private void Update() {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // Vector2 scaledMousePos = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);
        // Shader.SetGlobalVector("_MousePos", scaledMousePos);
        if(Physics.Raycast(cam.ScreenPointToRay(mousePos), out RaycastHit hit, 100))
        {
            if(hit.collider == null)
            {
                if(lastHovered != null)
                {
                    if(lastHovered.gameObject.TryGetComponent<IObjectButton>(out IObjectButton objectButton))
                        objectButton.HoverExit();
                    lastHovered.Reset();
                    lastHovered = null;
                }
                return;
            }
            
            if(hit.collider.gameObject.TryGetComponent<LiftOnHover>(out LiftOnHover hithoverer) && hithoverer.enabled)
            {
                if(hithoverer != lastHovered)
                {
                    lastHovered?.Reset();
                    hithoverer.Lift();
                    if(hithoverer.gameObject.TryGetComponent<IObjectButton>(out IObjectButton objectButton))
                        objectButton.HoverEnter();
                    lastHovered = hithoverer;
                }
            }
            else if(lastHovered != null)
            {
                if(lastHovered.gameObject.TryGetComponent<IObjectButton>(out IObjectButton objectButton))
                    objectButton.HoverExit();
                lastHovered.Reset();
                lastHovered = null;
            }
        }
        else if(lastHovered != null)
        {
            if(lastHovered.gameObject.TryGetComponent<IObjectButton>(out IObjectButton objectButton))
                objectButton.HoverExit();
            lastHovered.Reset();
            lastHovered = null;
        }
    }
    public void MouseClick(CallbackContext context)
    {
        if(!context.performed)
            return;
        
        if(lastHovered != null)
        {
            IObjectButton[] buttons = lastHovered.gameObject.GetComponents<IObjectButton>();
            foreach(IObjectButton button in buttons)
                button.Click();
        }
    }
}