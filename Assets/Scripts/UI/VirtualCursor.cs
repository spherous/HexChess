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
    MouseData mouseData;
    SmoothHalfOrbitalCamera smoothCamera;
    OnMouse onMouse;
    OptionsPanel optionsPanel;
    Board board;
    TurnHistoryPanel historyPanel;

    public CursorType currentType {get; private set;}

    private void Awake()
    {
        VirtualCursor[] allCursors = GameObject.FindObjectsOfType<VirtualCursor>();
        if(allCursors == null || allCursors.Length <= 1)
            DontDestroyOnLoad(gameObject);
        else
            Destroy(gameObject);

        TryFetchData();
    }

    private void Start() => SetCursor(CursorType.Default);
    
    private void OnEnable() => SceneManager.sceneLoaded += SceneChanged;
    private void OnDisable() => SceneManager.sceneLoaded -= SceneChanged;
    private void OnDestroy() {
        if(mouseData != null)
            mouseData.onHoverIPiece -= OnHoverIPiece;
    }

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

    private void SceneChanged(Scene arg0, LoadSceneMode arg1) => TryFetchData();

    private void TryFetchData()
    {
        TryFetchMouseData();
        TryFetchCamera();
        optionsPanel ??= GameObject.FindObjectOfType<OptionsPanel>();
        board ??= GameObject.FindObjectOfType<Board>();
        historyPanel ??= GameObject.FindObjectOfType<TurnHistoryPanel>();
    }

    private void TryFetchMouseData()
    {
        if(mouseData == null)
        {
            mouseData = GameObject.FindObjectOfType<MouseData>();
            if(mouseData != null)
                mouseData.onHoverIPiece += OnHoverIPiece;
        }

        onMouse ??= GameObject.FindObjectOfType<OnMouse>();
    }

    private void TryFetchCamera()
    {
        Camera mainCam = Camera.main;
        cam = mainCam.transform.childCount == 0 ? mainCam : mainCam.transform.GetChild(0).GetComponent<Camera>();
        smoothCamera ??= GameObject.FindObjectOfType<SmoothHalfOrbitalCamera>();
    }

    private void OnHoverIPiece(IPiece hoveredIPiece)
    {
        // No cursor is being shown then, maybe later add a sphere spinny icon?
        if(smoothCamera != null && smoothCamera.freeLooking)
            return;
        // Player is already holding a piece, so should be using grabby hand, lets not change that to an open hand
        else if(onMouse != null && onMouse.isPickedUp)
            return;
        // Options covers the board, so don't change based on board info while it's open
        else if(optionsPanel != null && optionsPanel.visible)
            return;
        // While viewing moves that have already happened, do not change the cursor to a hand.
        // When implementing lines, this will change.
        else if(historyPanel != null && !historyPanel.isShowingCurrentTurn)
            return;
        // When the player is drawing an arrow, we do not want to change the cursor
        else if(currentType != CursorType.Pencil) 
            return;
        else if(board == null)
            return;
        else if(!board.IsPlayerTurn())
            return;

        // If the piece is on our team, become hand
        if(hoveredIPiece != null && hoveredIPiece.team == board.GetCurrentTurn())
            SetCursor(CursorType.Hand);
        else
            SetCursor(CursorType.Default);
    }

    private void Update() {
        // The virtual cursor must always be at the position of the mouse
        Vector2 mousePos = Mouse.current.position.ReadValue() + cursors[(int)currentType].hotspotOffset;
        Vector2 scaledMousePos = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);
        Shader.SetGlobalVector("_MousePos", scaledMousePos);
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