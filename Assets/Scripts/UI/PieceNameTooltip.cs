using UnityEngine;
using UnityEngine.InputSystem;
using Extensions;
using TMPro;
using static UnityEngine.Camera;

public class PieceNameTooltip : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private GroupFader fader;
    [SerializeField] private Board board;
    [SerializeField] private TextMeshProUGUI pieceNameText;
    private Multiplayer multiplayer;
    private HandicapOverlayToggle singlePlayerHandicapOverlayToggle;
    public Vector2 offset;
    public LayerMask hexMask;

    private Mouse mouse = Mouse.current;

    public float showDelay;
    private float? showAtTime;
    private (Team team, Piece piece)? hoveredTeamedPiece;

    public bool blockDisplay;

    bool visible = false;

    private void Awake() {
        cam = Camera.main;
        multiplayer = GameObject.FindObjectOfType<Multiplayer>();
        if(multiplayer == null)
            singlePlayerHandicapOverlayToggle = GameObject.FindObjectOfType<HandicapOverlayToggle>();
    }

    private void Update() {
        if(multiplayer != null && !multiplayer.gameParams.showMovePreviews)
            return;
        else if(singlePlayerHandicapOverlayToggle != null && !singlePlayerHandicapOverlayToggle.toggle.isOn) 
            return;

        if(blockDisplay)
            return;

        if(fader.visible)
            UpdatePosition();
        
        if(showAtTime.HasValue && Time.timeSinceLevelLoad >= showAtTime.Value && hoveredTeamedPiece.HasValue)
        {
            Show(hoveredTeamedPiece.Value.piece.GetPieceLongString());
            showAtTime = null;
        }

        if(Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, 100, hexMask))
        {
            if(hit.collider == null)
                return;
            
            if(hit.collider.TryGetComponent<Hex>(out Hex hex))
            {
                BoardState currentState = board.GetCurrentBoardState();
                if(currentState.TryGetPiece(hex.index, out var teamedPiece))
                {
                    if(hoveredTeamedPiece != teamedPiece)
                    {
                        Hide();
                        hoveredTeamedPiece = teamedPiece;
                        showAtTime = Time.timeSinceLevelLoad + showDelay;
                    }
                }
                else
                    Hide();
            }
            else 
                Hide();
        }
        else
            Hide();
    }

    private void UpdatePosition()
    {
        Vector2 mousePos = mouse.position.ReadValue() + offset;
        transform.position = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 1), MonoOrStereoscopicEye.Mono);
    }

    public void Show(string pieceName)
    {
        pieceNameText.text = pieceName;        
        UpdatePosition();
        if(!visible)
        {
            visible = true;
            fader.FadeIn();
        }
    }
    public void Hide()
    {
        showAtTime = null;
        hoveredTeamedPiece = null;
        if(visible)
        {
            visible = false;
            fader.FadeOut();
        }
    }
}