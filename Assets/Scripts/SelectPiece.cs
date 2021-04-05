using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
using System.Linq;

public class SelectPiece : MonoBehaviour
{
    Mouse mouse => Mouse.current;
    Multiplayer multiplayer;
    Camera cam;
    [SerializeField] private Board board;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Color selectedPieceColor;
    public IPiece selectedPiece {get; private set;}
    [SerializeField] private OnMouse onMouse;
    IEnumerable<(Hex, MoveType)> pieceMoves = Enumerable.Empty<(Hex, MoveType)>();
    IEnumerable<(Hex, MoveType)> previewMoves = Enumerable.Empty<(Hex, MoveType)>();
    public List<Color> moveTypeHighlightColors = new List<Color>();
    public Color previewColor;
    public Color okayColor;
    public Color errColor;

    MeshRenderer lastChangedRenderer;
    Color lastColor;

    private void Awake() 
    {
        cam = Camera.main;
        multiplayer = GameObject.FindObjectOfType<Multiplayer>();
    }
    private void Update()
    {
        HighlightOnHover();
        ColorizeBasedOnMove();

        if(selectedPiece != null && !Cursor.visible)
            DeselectPiece(selectedPiece.location);
    }

    private void ColorizeBasedOnMove()
    {
        if(selectedPiece != null && onMouse != null)
        {
            if(Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, layerMask))
            {
                IPiece hitPiece = hit.collider.GetComponent<IPiece>();
                if(hitPiece != null)
                {
                    Hex hex = board.GetHexIfInBounds(hitPiece.location);
                    if(hex != null)
                    {
                        SetOnMouseColor(hex);

                        #region Change color of other piece under mouse
                        MeshRenderer hitRenderer = hitPiece.obj.GetComponentInChildren<MeshRenderer>();
                        if(hitPiece != selectedPiece && hitRenderer != null)
                        {
                            if(hitRenderer != lastChangedRenderer)
                            {
                                lastColor = hitRenderer.material.GetColor("_BaseColor");
                                Color toSet;
                                if(pieceMoves.Contains((hex, MoveType.Attack)) || pieceMoves.Contains((hex, MoveType.EnPassant)))
                                    toSet = errColor;
                                else if(pieceMoves.Contains((hex, MoveType.Defend)))
                                    toSet = okayColor;
                                else
                                {
                                    if(lastChangedRenderer != null)
                                        ResetLastChangedRenderer();
                                    return;
                                }

                                hitRenderer.material.SetColor("_BaseColor", toSet);
                                lastChangedRenderer = hitRenderer;
                            }
                        }
                        else if(lastChangedRenderer != null)
                            ResetLastChangedRenderer();
                        #endregion
                    }
                }
                else
                {
                    Hex hitHex = hit.collider.GetComponent<Hex>();
                    if(hitHex != null)
                    {
                        SetOnMouseColor(hitHex);

                        #region Change color of other piece under mouse
                        BoardState currentBoardState = board.GetCurrentBoardState();
                        if(currentBoardState.allPiecePositions.ContainsKey(hitHex.index))
                        {
                            (Team occupyingTeam, Piece occupyingPiece) = currentBoardState.allPiecePositions[hitHex.index];
                            if(board.activePieces.ContainsKey((occupyingTeam, occupyingPiece)))
                            {
                                IPiece piece = board.activePieces[(occupyingTeam, occupyingPiece)];
                                if(piece != selectedPiece)
                                {
                                    MeshRenderer hitPieceRenderer = piece.obj.GetComponentInChildren<MeshRenderer>();
                                    if(hitPieceRenderer != lastChangedRenderer)
                                    {
                                        lastColor = hitPieceRenderer.material.GetColor("_BaseColor");

                                        Color toSet;
                                        if(pieceMoves.Contains((hitHex, MoveType.Attack)) || pieceMoves.Contains((hitHex, MoveType.EnPassant)))
                                            toSet = errColor;
                                        else if(pieceMoves.Contains((hitHex, MoveType.Defend)))
                                            toSet = okayColor;
                                        else
                                        {
                                            if(lastChangedRenderer != null)
                                                ResetLastChangedRenderer();
                                            return;
                                        }

                                        hitPieceRenderer.material.SetColor("_BaseColor", toSet);
                                        lastChangedRenderer = hitPieceRenderer;
                                    }
                                }
                                else if(lastChangedRenderer != null)
                                    ResetLastChangedRenderer();
                            }
                            else if(lastChangedRenderer != null)
                                ResetLastChangedRenderer();
                        }
                        else if(lastChangedRenderer != null)
                            ResetLastChangedRenderer();
                        #endregion
                    }
                }
            }
        }
    }

    private void SetOnMouseColor(Hex hex)
    {
        if(pieceMoves.Contains((hex, MoveType.Attack))
            || pieceMoves.Contains((hex, MoveType.Move))
            || pieceMoves.Contains((hex, MoveType.Defend))
            || pieceMoves.Contains((hex, MoveType.EnPassant))
        )
        {
            if(onMouse.currentColor != okayColor)
                onMouse.SetColor(okayColor);
        }
        else if(onMouse.currentColor != errColor)
            onMouse.SetColor(errColor);
    }

    private void ResetLastChangedRenderer()
    {
        lastChangedRenderer?.material.SetColor("_BaseColor", lastColor);
        lastChangedRenderer = null;
    }

    private void HighlightOnHover()
    {
        if(multiplayer != null)
        {
            if(!multiplayer.gameParams.showMovePreviews)
                return;
        }

        // Add a toggle for sandbox mode to turn these on/off, if off, return here

        if(selectedPiece == null)
        {
            if(Cursor.visible && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, layerMask))
            {
                BoardState currentBoardState = board.GetCurrentBoardState();
                IPiece hoveredPiece = hit.collider.GetComponent<IPiece>();
                if(hoveredPiece == null)
                {
                    Hex hoveredHex = hit.collider.GetComponent<Hex>();
                    if(hoveredHex != null)
                    {
                        if(currentBoardState.allPiecePositions.ContainsKey(hoveredHex.index))
                        {
                            // Get the piece on that hex
                            hoveredPiece = board.activePieces[currentBoardState.allPiecePositions[hoveredHex.index]];
                            if(hoveredPiece != null && !hoveredPiece.captured)
                            {
                                IEnumerable<(Hex, MoveType)> incomingPreviewMoves = board.GetAllValidMovesForPiece(
                                    hoveredPiece,
                                    currentBoardState
                                );
                                if(incomingPreviewMoves != previewMoves)
                                {
                                    DisablePreview();
                                    previewMoves = incomingPreviewMoves;
                                    previewMoves = previewMoves.Append((hoveredHex, MoveType.None));
                                    EnablePreview();
                                }
                            }
                            else if(previewMoves.Count() > 0)
                                DisablePreview();
                        }
                        else if(previewMoves.Count() > 0)
                            DisablePreview();
                    }
                    else if(previewMoves.Count() > 0)
                        DisablePreview();
                }
                else if(!hoveredPiece.captured)
                {
                    IEnumerable<(Hex, MoveType)> incomingPreviewMoves = board.GetAllValidMovesForPiece(hoveredPiece, currentBoardState);
                    if(incomingPreviewMoves != previewMoves)
                    {
                        DisablePreview();
                        previewMoves = incomingPreviewMoves;

                        Hex hoveredPieceHex = board.GetHexIfInBounds(hoveredPiece.location);
                        if(hoveredPieceHex != null)
                            previewMoves = previewMoves.Append((hoveredPieceHex, MoveType.None));

                        EnablePreview();
                    }
                }
                else if(previewMoves.Count() > 0)
                    DisablePreview();
            }
            else if(previewMoves.Count() > 0)
                DisablePreview();
        }
        else if(previewMoves.Count() > 0)
            DisablePreview();
    }

    private void EnablePreview()
    {
        foreach((Hex hex, MoveType moveType) in previewMoves)
        {
            hex.SetOutlineColor(previewColor);
            hex.ToggleSelect();
        }
    }

    private void DisablePreview()
    {
        foreach ((Hex hex, MoveType moveType) in previewMoves)
            hex.ToggleSelect();
        previewMoves = Enumerable.Empty<(Hex, MoveType)>();
    }

    public void LeftClick(CallbackContext context)
    {
        if(context.started)
        {
            // Later allow players to queue a move, but for now, just prevent even clicking a piece when not their turn
            if(multiplayer != null && multiplayer.gameParams.localTeam != board.GetCurrentTurn())
                return;

            if(Cursor.visible && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, layerMask))
            {
                if(hit.collider == null)
                    return;
                
                BoardState currentBoardState = board.GetCurrentBoardState();
                IPiece clickedPiece = hit.collider.GetComponent<IPiece>();
                if(clickedPiece != null && !clickedPiece.captured && clickedPiece.team == currentBoardState.currentMove)
                {
                    if(selectedPiece != null)
                        DeselectPiece(selectedPiece.location);

                    // Select new piece and highlight all of the places it can move to on the current board state
                    selectedPiece = clickedPiece;
                    pieceMoves = board.GetAllValidMovesForPiece(selectedPiece, currentBoardState);
                    // Highlight each possible move the correct color
                    foreach((Hex hex, MoveType moveType) in pieceMoves)
                    {
                        hex.SetOutlineColor(moveTypeHighlightColors[(int)moveType]);
                        hex.ToggleSelect();
                    }

                    Hex selectedHex = board.GetHexIfInBounds(selectedPiece.location);
                    selectedHex.SetOutlineColor(selectedPieceColor);
                    selectedHex.ToggleSelect();
                    onMouse.PickUp(selectedPiece.obj);
                }
            }
        }
        else if(context.canceled)
        {
            if(lastChangedRenderer != null)
                ResetLastChangedRenderer();

            if(Cursor.visible && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, layerMask))
            {
                BoardState currentBoardState = board.GetCurrentBoardState();
                IPiece clickedPiece = hit.collider.GetComponent<IPiece>();

                if(clickedPiece != null && selectedPiece != null)
                {
                    // Rooks can defend (swap positions with a near by ally)
                    if(clickedPiece.team == selectedPiece.team && pieceMoves.Contains((board.GetHexIfInBounds(clickedPiece.location), MoveType.Defend)))
                    {
                        Defend(clickedPiece);
                        return;
                    }
                    else
                    {
                        Hex enemyHex = board.GetHexIfInBounds(clickedPiece.location);
                        // Check if this attack is within our possible moves
                        if(pieceMoves.Contains((enemyHex, MoveType.Attack)))
                            MoveOrAttack(enemyHex);
                    }
                }

                // Clicked on a hex
                Hex hitHex = hit.collider.GetComponent<Hex>();
                if(hitHex != null && selectedPiece != null)
                {
                    if(pieceMoves.Contains((hitHex, MoveType.Attack)) || pieceMoves.Contains((hitHex, MoveType.Move)))
                        MoveOrAttack(hitHex);
                    else if(pieceMoves.Contains((hitHex, MoveType.Defend)))
                        Defend(board.activePieces[currentBoardState.allPiecePositions[hitHex.index]]);
                    else if(pieceMoves.Contains((hitHex, MoveType.EnPassant)))
                        EnPassant(currentBoardState, hitHex);
                }
            }

            if(selectedPiece != null)
                DeselectPiece(selectedPiece.location);
        }
    }

    private void MoveOrAttack(Hex hitHex)
    {
        Index pieceStartLoc = selectedPiece.location;
        BoardState newState = board.MovePiece(selectedPiece, hitHex, board.GetCurrentBoardState());
        
        if(multiplayer != null)
        {
            // We skip sending a board here when a promotion is happening. That will be sent with the promotion after it's chosen
            if(!(selectedPiece is Pawn pawn) || pawn.goal != hitHex.index.row)
                multiplayer.SendBoard(newState);
        }

        board.AdvanceTurn(newState);
        DeselectPiece(pieceStartLoc);
    }

    private void Defend(IPiece pieceToDefend)
    {
        Index startLoc = selectedPiece.location;
        // Hex startHex = board.GetHexIfInBounds(startLoc.row, startLoc.col);
        BoardState newState = board.Swap(selectedPiece, pieceToDefend, board.GetCurrentBoardState());
        if(multiplayer != null)
            multiplayer.SendBoard(newState);
        board.AdvanceTurn(newState);
        DeselectPiece(startLoc);
    }

    private void EnPassant(BoardState currentBoardState, Hex hitHex)
    {
        int teamOffset = currentBoardState.currentMove == Team.White ? -2 : 2;
        Index enemyLoc = new Index(hitHex.index.row + teamOffset, hitHex.index.col);
        (Team enemyTeam, Piece enemyType) = currentBoardState.allPiecePositions[enemyLoc];
        BoardState newState = board.EnPassant((Pawn)selectedPiece, enemyTeam, enemyType, hitHex, currentBoardState);

        if (multiplayer != null)
            multiplayer.SendBoard(newState);

        board.AdvanceTurn(newState);
        DeselectPiece(selectedPiece.location);
    }

    private int GetGoal(Team team, int row) => team == Team.White ? 18 - (row % 2) : row % 2;

    public void DeselectPiece(Index fromIndex)
    {
        if(selectedPiece == null)
            return;

        foreach((Hex hex, MoveType moveType) in pieceMoves)
            hex.ToggleSelect();
        pieceMoves = Enumerable.Empty<(Hex, MoveType)>();

        board.GetHexIfInBounds(fromIndex).ToggleSelect();
        onMouse.PutDown();
        
        selectedPiece = null;
    }
}