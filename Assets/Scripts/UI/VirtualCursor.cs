using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static UnityEngine.Camera;

public class VirtualCursor : MonoBehaviour
{
    [SerializeField] private List<CursorData> cursors = new List<CursorData>();
    [SerializeField] private SpriteRenderer spriteRenderer;

    public bool visible {get; private set;} = true;

    Camera cam;
    public CursorType currentType {get; private set;}

    private void Awake()
    {
        VirtualCursor[] allCursors = GameObject.FindObjectsOfType<VirtualCursor>();
        if(allCursors == null || allCursors.Length <= 1)
            DontDestroyOnLoad(gameObject);
        else
            Destroy(gameObject);
    } 
    private void OnEnable() => SceneManager.sceneLoaded += SceneChanged;
    private void OnDisable() => SceneManager.sceneLoaded -= SceneChanged;
    private void Start() => SetCursor(CursorType.Default);

    public void Hide()
    {
        visible = false;
        SetCursor(CursorType.None);
    }

    public void Show()
    {
        visible = true;
        SetCursor(CursorType.Default, true);
    } 

    private void SceneChanged(Scene arg0, LoadSceneMode arg1)
    {
        Camera mainCam = Camera.main;
        cam = mainCam.transform.childCount == 0 ? mainCam : mainCam.transform.GetChild(0).GetComponent<Camera>();
    }

    private void Update() {
        // The virtual cursor must always be at the position of the mouse
        Vector2 mousePos = Mouse.current.position.ReadValue() + cursors[(int)currentType].hotspotOffset;
        Vector3 pos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 1), MonoOrStereoscopicEye.Mono);
        transform.position = pos;
        transform.forward = cam.transform.forward;

#if UNITY_EDITOR
        // The cursor may be hidden while in edit mode, this is fine until the cursor exits the game window, then we need to enable it for the editor
        // And disable it when the cursor re-enters the game window
        if(visible)
        {
            bool isOutOfFrame = mousePos.x < 0 || mousePos.y < 0 || mousePos.x > Screen.width || mousePos.y > Screen.height;
            if(isOutOfFrame && !Cursor.visible)
                Cursor.visible = true;
            else if(!isOutOfFrame && Cursor.visible && currentType != CursorType.None)
                Cursor.visible = false;
        }
#endif
    }

    public void SetCursor(CursorType type, bool force = false)
    {
        if(type == currentType && !force)
            return;

        CursorData toUse = cursors[(int)type];
        if(type == CursorType.None)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = visible;
            spriteRenderer.enabled = false;
        }
        else
        {
            if(currentType == CursorType.None && Cursor.visible)
                Cursor.visible = false;
            spriteRenderer.enabled = visible;
            spriteRenderer.sprite = toUse.cursor;
        }
        
        currentType = type;
    }
}

public enum CursorType {None, Default, Hand, Grab, Pencil}