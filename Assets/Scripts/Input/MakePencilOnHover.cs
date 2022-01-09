using UnityEngine;
using UnityEngine.EventSystems;

public class MakePencilOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    VirtualCursor virtualCursor;

    private void Awake() => virtualCursor = GameObject.FindObjectOfType<VirtualCursor>();
    public void OnPointerEnter(PointerEventData eventData) => virtualCursor?.SetCursor(CursorType.Pencil);
    public void OnPointerExit(PointerEventData eventData) => virtualCursor?.SetCursor(CursorType.Default);
}