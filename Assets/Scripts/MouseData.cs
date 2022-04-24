using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseData : MonoBehaviour
{
    Camera cam;
    Mouse mouse;
    Board board;

    [SerializeField] private LayerMask layerMask;

    [ReadOnly, ShowInInspector] public Hex hoveredHex {get; private set;} = null;
    [ReadOnly, ShowInInspector] public IPiece hoveredIPiece {get; private set;} = null;
    [ReadOnly, ShowInInspector] public TextMeshPro hoveredKey {get; private set;} = null;

    public delegate void OnHoverHex(Hex hoveredHex);
    public delegate void OnHoverIPiece(IPiece hoveredIPiece);
    public delegate void OnHoverKey(TextMeshPro hoveredKey);
    public OnHoverHex onHoverHex;
    public OnHoverIPiece onHoverIPiece;
    public OnHoverKey onHoverKey;

    private void Awake()
    {
        cam = Camera.main;
        mouse = Mouse.current;
        board = GameObject.FindObjectOfType<Board>();
    }

    void Update()
    {
        if(Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, 100, layerMask))
        {
            if(hit.transform.TryGetComponent<Hex>(out Hex hitHex) && hitHex.isGameHex)
            {
                if(hoveredHex != hitHex)
                    SetHex(hitHex);

                if(board.TryGetIPieceFromIndex(hitHex.index, out IPiece piece))
                {
                    if(hoveredIPiece != piece)
                        SetIPiece(piece);
                }
                else
                    SetIPiece();
            }
            else
                SetHex();

            if(hit.transform.TryGetComponent<TextMeshPro>(out TextMeshPro hitKey))
            {
                if(hoveredKey != hitKey)
                    SetKey(hitKey);
            }
            else
                SetKey();
        }
        else
            ClearAllData();
    }

    private void ClearAllData()
    {
        SetHex();
        SetIPiece();
        SetKey();
    }

    private void SetKey(TextMeshPro key = null)
    {
        hoveredKey = key;
        onHoverKey?.Invoke(hoveredKey);
    }

    private void SetIPiece(IPiece piece = null)
    {
        hoveredIPiece = piece;
        onHoverIPiece?.Invoke(hoveredIPiece);
    }

    private void SetHex(Hex hex = null)
    {
        hoveredHex = hex;
        onHoverHex?.Invoke(hoveredHex);
    }
}