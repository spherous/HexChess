using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class OnMouse : MonoBehaviour
{
    [SerializeField] private MeshRenderer pickedUpRenderer;
    [SerializeField] private MeshFilter filter;
    Camera cam;
    public float distance;
    public Color? currentColor {get; private set;}

    public bool isPickedUp {get; private set;} = false;
    
    private void Awake() => cam = Camera.main;

    private void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        transform.position = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, distance));
    }

    public void PickUp(GameObject toPickup) 
    {
        MeshFilter toPickupFilter = toPickup.GetComponentInChildren<MeshFilter>();
        filter.mesh = toPickupFilter.mesh;
        filter.transform.rotation = toPickupFilter.transform.rotation;
        isPickedUp = true;
    } 

    public void PutDown()
    {
        currentColor = null;
        filter.mesh = null;
        isPickedUp = false;
    }
    public void SetBaseColor(Color color)
    {
        if(filter.mesh == null)
            return;

        pickedUpRenderer.material.SetColor("_BaseColor", color);
        SetColor(color);
    }

    public void SetColor(Color color)
    {
        if(filter.mesh == null)
            return;
        
        if(currentColor.HasValue && currentColor.Value == color)
            return;
        
        currentColor = color;
        pickedUpRenderer.material.SetColor("_HighlightColor", color);
    }
}