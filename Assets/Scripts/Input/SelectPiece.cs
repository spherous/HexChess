using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
using System.Linq;
using TMPro;
using Sirenix.OdinInspector;

public class SelectPiece : MonoBehaviour
{
    Mouse mouse => Mouse.current;
    Multiplayer multiplayer;
    HandicapOverlayToggle singlePlayerHandicapOverlayToggle;
    Camera cam;
    [SerializeField] SmoothHalfOrbitalCamera smoothHalfOrbitalCamera;
    [SerializeField] private Board board;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private LayerMask hexMask;
    [SerializeField] private LayerMask keysMask;
    [SerializeField] private Color selectedPieceColor;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private PromotionDialogue promotionDialogue;
    [SerializeField] private TurnHistoryPanel historyPanel;
    [SerializeField] public ArrowTool arrowTool;
    [SerializeField] private AIBattleController aiController;
    [SerializeField] private GroupFader optionsPanel;
    public AudioClip cancelNoise;
    public AudioClip pickupNoise;
    public IPiece selectedPiece {get; private set;}
    [SerializeField] private OnMouse onMouse;
    private FreePlaceModeToggle freePlaceMode;
    [SerializeField] private LastMoveTracker lastMoveTracker;
    bool isFreeplaced => !multiplayer && freePlaceMode.toggle.isOn;
    private bool hoverExitedInitialHex = false;
    List<(Hex, MoveType)> pieceMoves = new List<(Hex, MoveType)>();
    List<(Hex, MoveType)> previewMoves = new List<(Hex, MoveType)>();
    public List<Color> moveTypeHighlightColors = new List<Color>();
    public Color greenColor;
    public Color redColor;
    public Color orangeColor;
    public Color hoverColor;
    public Color whiteColor;
    public Color blackColor;
    public Color yellowColor;
    [ShowInInspector, ReadOnly] List<Index> attacksConcerningHex = new List<Index>();
    Dictionary<IPiece, List<Index>> attacksConcerningHexDict = new Dictionary<IPiece, List<Index>>();
    IPiece lastChangedPiece;

    private Hex lastHoveredHex = null;
    private Hex lastHoveredHexForKeyHighlight = null;

    private Keys keys;

    private TextMeshPro lastHoveredKey = null;
    IEnumerable<Hex> keyHighlightedHexes = Enumerable.Empty<Hex>();

    Hex checkedKingHex = null;

    private VirtualCursor cursor;

    private void Awake() 
    {
        cam = Camera.main;
        multiplayer = GameObject.FindObjectOfType<Multiplayer>();
        if(multiplayer == null)
            singlePlayerHandicapOverlayToggle = GameObject.FindObjectOfType<HandicapOverlayToggle>();
        keys = GameObject.FindObjectOfType<Keys>();
        cursor = GameObject.FindObjectOfType<VirtualCursor>();
        freePlaceMode = GameObject.FindObjectOfType<FreePlaceModeToggle>();
        board.newTurn += NewTurn;
    }

    private void Start() => cursor?.SetCursor(CursorType.Default);

    private void NewTurn(BoardState newState)
    {
        attacksConcerningHexDict.Clear();

        ClearCheckOrMateHighlight();
        HighlightPotentialCheckOrMate(newState);
    }

    public void ClearCheckOrMateHighlight()
    {
        if(checkedKingHex != null)
        {
            Move lastMove = board.currentGame.GetLastMove(isFreeplaced);
            if(lastMove.from != checkedKingHex.index && lastMove.to != checkedKingHex.index)
                checkedKingHex.Unhighlight();
            checkedKingHex = null;
        }
    }

    public void HighlightPotentialCheckOrMate(BoardState state)
    {
        if(state.TryGetIndex((state.checkmate, Piece.King), out Index matedIndex))
        {
            checkedKingHex = board.GetHexIfInBounds(matedIndex);
            checkedKingHex.Highlight(redColor);
        }
        else if(state.TryGetIndex((state.check, Piece.King), out Index checkedIndex))
        {
            checkedKingHex = board.GetHexIfInBounds(checkedIndex);
            checkedKingHex.Highlight(yellowColor);
        }
    }

    private void Update()
    {
        HandicapOverlayOnHover();
        ColorizeBasedOnMove();
        HighlightKeysOnHoverHex();
        HighlightHexOnHoverKey();

        ChangeCursorOnHover();
    }

    private void ChangeCursorOnHover()
    {
        if(smoothHalfOrbitalCamera.freeLooking)
            return;

        if(onMouse.isPickedUp || cursor == null)
            return;

        if(optionsPanel != null && optionsPanel.visible)
            return;

        Team currentTurn = board.GetCurrentTurn();
        
        // Not this player's turn
        if(multiplayer != null && multiplayer.gameParams.localTeam != currentTurn)
            return;
        
        // Previewing an old move, don't make the player think they can play a move by giving hand cursor
        if(historyPanel.panelPointer != historyPanel.currentTurnPointer)
            return;
        
        // If it's the AI's turn, don't show the player the hand cursor
        if(aiController != null)
        {
            if(aiController.WhiteAIEnabled() && currentTurn == Team.White)
                return;
            if(aiController.BlackAIEnabled() && currentTurn == Team.Black)
                return;
        }

        if(Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, 100, hexMask))
        {
            if(hit.collider == null)
                return;

            BoardState currentBoardState = board.GetCurrentBoardState();
            if(hit.collider.TryGetComponent<Hex>(out Hex clickedHex) && currentBoardState.allPiecePositions.ContainsKey(clickedHex.index) && clickedHex.isGameHex)
            {
                if(currentBoardState.TryGetPiece(clickedHex.index, out (Team team, Piece piece) teamedPiece) && teamedPiece.team == currentBoardState.currentMove)
                {
                    if(cursor.currentType != CursorType.Pencil)
                        cursor.SetCursor(CursorType.Hand);
                }
                else
                {
                    if(cursor.currentType != CursorType.Pencil)
                        cursor.SetCursor(CursorType.Default);
                }
            }
            else if(cursor.currentType != CursorType.Pencil)
                cursor.SetCursor(CursorType.Default);
        }
        else if(cursor.currentType != CursorType.Pencil)
            cursor.SetCursor(CursorType.Default);
    }

    private void HighlightHexOnHoverKey()
    {
        if(onMouse.isPickedUp)
            return;
        else if(multiplayer != null && !multiplayer.gameParams.showMovePreviews)
            return;
        else if(singlePlayerHandicapOverlayToggle != null && !singlePlayerHandicapOverlayToggle.toggle.isOn) 
            return;
        else if(smoothHalfOrbitalCamera.freeLooking)
            return;
        else if(optionsPanel != null && optionsPanel.visible)
            return;


        if(Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, 100, keysMask))
        {
            if(hit.collider.TryGetComponent<TextMeshPro>(out TextMeshPro hitKey))
            {
                foreach(Hex hex in keyHighlightedHexes)
                    hex.ToggleSelect();
                
                keyHighlightedHexes = Enumerable.Empty<Hex>();
                lastHoveredKey = null;
            }
            
            if(lastHoveredKey == hitKey)
                return;

            foreach(Hex hex in keyHighlightedHexes)
                hex.ToggleSelect();
            
            keyHighlightedHexes = Enumerable.Empty<Hex>();
            
            if(int.TryParse(hitKey.text, out int hitNum))
            {
                // Fetch all hexes in proper rows
                (int desiredRow1, int desiredRow2) = hitNum switch {
                    1 => (0, 1), 2 => (2, 3),
                    3 => (4, 5), 4 => (6, 7),
                    5 => (8, 9), 6 => (10, 11),
                    7 => (12, 13), 8 => (14, 15),
                    9 => (16, 17), 10 => (18, 19),
                    _ => (-1, -1) 
                };

                if(desiredRow1 == -1)
                    return;
                
                IEnumerable<Hex> hexesInRow1 = board.hexes.Count > desiredRow1 ? board.hexes[desiredRow1] : Enumerable.Empty<Hex>();
                IEnumerable<Hex> hexesInRow2 = board.hexes.Count > desiredRow2 ? board.hexes[desiredRow2] : Enumerable.Empty<Hex>();
                IEnumerable<Hex> hexesInRow = hexesInRow1.Concat(hexesInRow2);

                foreach(Hex hex in hexesInRow)
                {
                    hex.SetOutlineColor(orangeColor);
                    hex.ToggleSelect();
                }
                keyHighlightedHexes = hexesInRow;
            }
            else
            {
                // Fetch all hexes in proper col
                (bool isEven, int c) = hitKey.text switch {
                    "A" => (false, 0), "B" => (true, 0),
                    "C" => (false, 1), "D" => (true, 1),
                    "E" => (false, 2), "F" => (true, 2),
                    "G" => (false, 3), "H" => (true, 3),
                    "I" => (false, 4), _ => (false, -1)
                };

                if(c == -1)
                    return;
                
                IEnumerable<Hex> hexesInCol = board.GetHexesInCol(c);
                hexesInCol = hexesInCol.Where(hex => hex.index.row % 2 == 0 == isEven);
                foreach(Hex hex in hexesInCol)
                {
                    hex.SetOutlineColor(orangeColor);
                    hex.ToggleSelect();
                }
                keyHighlightedHexes = hexesInCol;
            }

            lastHoveredKey = hitKey;
        }
        else
        {
            foreach(Hex hex in keyHighlightedHexes)
                hex.ToggleSelect();
            
            keyHighlightedHexes = Enumerable.Empty<Hex>();
            lastHoveredKey = null;
        }
    }

    private void HighlightKeysOnHoverHex()
    {
        if(smoothHalfOrbitalCamera.freeLooking)
            return;
        else if(optionsPanel != null && optionsPanel.visible)
            return;

        if(Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, 100, hexMask))
        {
            if(hit.collider == null)
            {   
                if(lastHoveredHexForKeyHighlight != null)
                {
                    keys.Clear();
                    lastHoveredHexForKeyHighlight = null;
                }
                return;
            }
            Hex hoveredHex = hit.collider.GetComponent<Hex>();
            if(hoveredHex == null && hoveredHex.isGameHex)
            {
                if(lastHoveredHexForKeyHighlight != null)
                {
                    keys.Clear();
                    lastHoveredHexForKeyHighlight = null;
                }
                return;
            }
            else if(hoveredHex == lastHoveredHexForKeyHighlight)
                return;
            
            if(hoveredHex.isGameHex)
            {
                keys.HighlightKeys(hoveredHex.index);
                lastHoveredHexForKeyHighlight = hoveredHex;
            }
            else if(lastHoveredHexForKeyHighlight != null)
            {
                keys.Clear();
                lastHoveredHexForKeyHighlight = null;
            }
        }
        else if(lastHoveredHexForKeyHighlight != null)
        {
            keys.Clear();
            lastHoveredHexForKeyHighlight = null;
        }
    }

    private void ColorizeBasedOnMove()
    {
        if(smoothHalfOrbitalCamera.freeLooking)
            return;
        else if(optionsPanel != null && optionsPanel.visible)
            return;

        if(selectedPiece != null && onMouse != null)
        {
            if(Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, 100, layerMask))
            {
                if(hit.collider.TryGetComponent<IPiece>(out IPiece hitPiece))
                {
                    if(board.TryGetHexIfInBounds(hitPiece.location, out Hex hex))
                    {
                        SetOnMouseColor(hex);

                        if(hitPiece != selectedPiece)
                        {
                            if(hitPiece != lastChangedPiece)
                            {
                                if(lastChangedPiece != null)
                                    ResetLastChangedRenderer();

                                Color toSet = hitPiece.team == Team.White ? whiteColor : blackColor;
                                if(pieceMoves.Contains((hex, MoveType.Attack)) || pieceMoves.Contains((hex, MoveType.EnPassant)))
                                    toSet = redColor;
                                else if(pieceMoves.Contains((hex, MoveType.Defend)) || pieceMoves.Contains((hex, MoveType.Move)))
                                    toSet = greenColor;

                                hitPiece.HighlightWithColor(toSet);
                                lastChangedPiece = hitPiece;
                            }
                        }
                        else if(lastChangedPiece != null)
                            ResetLastChangedRenderer();
                    }
                }
                else
                {
                    Hex hitHex = hit.collider.GetComponent<Hex>();
                    if(hitHex != null && hitHex.isGameHex)
                    {
                        SetOnMouseColor(hitHex);
                        BoardState currentBoardState = board.GetCurrentBoardState();
                        if(currentBoardState.TryGetPiece(hitHex.index, out (Team occupyingTeam, Piece occupyingPiece) teamedPiece))
                        {
                            if(board.activePieces.ContainsKey((teamedPiece.occupyingTeam, teamedPiece.occupyingPiece)))
                            {
                                IPiece piece = board.activePieces[teamedPiece];
                                if(piece != selectedPiece)
                                {
                                    if(piece != lastChangedPiece)
                                    {
                                        if(lastChangedPiece != null)
                                            ResetLastChangedRenderer();

                                        Color toSet = piece.team == Team.White ? whiteColor : blackColor;
                                        if(pieceMoves.Contains((hitHex, MoveType.Attack)) || pieceMoves.Contains((hitHex, MoveType.EnPassant)))
                                            toSet = redColor;
                                        else if(pieceMoves.Contains((hitHex, MoveType.Defend)) || pieceMoves.Contains((hitHex, MoveType.Move)))
                                            toSet = greenColor;

                                        piece.HighlightWithColor(toSet);
                                        lastChangedPiece = piece;
                                    }
                                }
                                else if(lastChangedPiece != null)
                                    ResetLastChangedRenderer();
                            }
                            else if(lastChangedPiece != null)
                                ResetLastChangedRenderer();
                        }
                        else if(lastChangedPiece != null)
                            ResetLastChangedRenderer();
                    }
                    else
                        SetOnMouseColor();
                }
            }
            else
                SetOnMouseColor();
        }
    }

    private void SetOnMouseColor(Hex hex = null)
    {
        if(hex != null)
        {
            if(pieceMoves.Contains((hex, MoveType.Attack))
                || pieceMoves.Contains((hex, MoveType.Move))
                || pieceMoves.Contains((hex, MoveType.Defend))
                || pieceMoves.Contains((hex, MoveType.EnPassant))
            )
            {
                onMouse.SetColor(greenColor);
                hoverExitedInitialHex = true;
            }
            else if(selectedPiece.location == hex.index && !hoverExitedInitialHex)
                onMouse.SetColor(selectedPiece.team == Team.White ? whiteColor : blackColor);
            else
            {
                onMouse.SetColor(redColor);
                hoverExitedInitialHex = true;
            }
        }
        else
        {
            onMouse.SetColor(redColor);
            hoverExitedInitialHex = true;
        }
    }

    private void ResetLastChangedRenderer()
    {
        lastChangedPiece?.ResetHighlight();
        lastChangedPiece = null;
    }

    private void HandicapOverlayOnHover()
    {
        if(multiplayer != null && !multiplayer.gameParams.showMovePreviews)
            return;
        else if(singlePlayerHandicapOverlayToggle != null && !singlePlayerHandicapOverlayToggle.toggle.isOn) 
            return;
        else if(promotionDialogue.gameObject.activeSelf || smoothHalfOrbitalCamera.freeLooking)
            return;
        else if(optionsPanel != null && optionsPanel.visible)
            return;

        if(selectedPiece == null)
        {
            bool cursorVisability = cursor != null ? cursor.visible : true;
            if(cursorVisability && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, 100, hexMask))
            {
                BoardState currentBoardState = board.GetCurrentBoardState();
                IPiece hoveredPiece = null;
                Hex hoveredHex = hit.collider.GetComponent<Hex>();
                if(hoveredHex != null && hoveredHex.isGameHex)
                {
                    if(lastHoveredHex == hoveredHex)
                        return;
                    
                    lastHoveredHex = hoveredHex;
                    if(currentBoardState.allPiecePositions.ContainsKey(hoveredHex.index))
                    {
                        // Get the piece on that hex
                        (Team t, Piece p) = currentBoardState.allPiecePositions[hoveredHex.index];
                        if(board.activePieces.ContainsKey((t, p)))
                            hoveredPiece = board.activePieces[(t, p)];
                        if(hoveredPiece != null && !hoveredPiece.captured)
                        {
                            IEnumerable<(Hex targetIndex, MoveType moveType)> incomingPreviewMoves = board.currentGame.GetAllValidMovesForPiece(
                                (hoveredPiece.team, hoveredPiece.piece),
                                currentBoardState,
                                true
                            ).Select(kvp => (board.GetHexIfInBounds(kvp.target), kvp.moveType));

                            DisablePreview();
                            previewMoves = incomingPreviewMoves.ToList();
                            previewMoves.Add((hoveredHex, MoveType.None));

                            List<Index> validAttacksOnHex = board.currentGame.GetValidAttacksConcerningHex(hoveredHex.index, currentBoardState).ToList();

                            if(!attacksConcerningHexDict.ContainsKey(hoveredPiece))
                                attacksConcerningHexDict.Add(hoveredPiece, validAttacksOnHex);
                            else if(attacksConcerningHexDict[hoveredPiece] != validAttacksOnHex)
                            {
                                attacksConcerningHexDict.Remove(hoveredPiece);
                                attacksConcerningHexDict.Add(hoveredPiece, validAttacksOnHex);
                            }

                            EnablePreview();
                        }
                        else if(previewMoves.Count > 0)
                            DisablePreview();
                    }
                    else if(previewMoves.Count > 0)
                        DisablePreview();
                }
                else if(previewMoves.Count > 0)
                {
                    lastHoveredHex = null;
                    DisablePreview();
                }
                else if(previewMoves.Count > 0)
                    DisablePreview();
            }
            else if(previewMoves.Count > 0)
            {
                DisablePreview();
                lastHoveredHex = null;
            }
        }
        else if(previewMoves.Count > 0)
            DisablePreview();
    }

    private void ColorizePieces()
    {
        BoardState boardState = board.GetCurrentBoardState();
        IPiece hoveredPiece = board.activePieces[boardState.allPiecePositions[lastHoveredHex.index]];
        if(!attacksConcerningHexDict.ContainsKey(hoveredPiece)) 
            return;

        attacksConcerningHex = attacksConcerningHexDict[hoveredPiece];
        foreach(Index index in attacksConcerningHex)
        {
            if(boardState.TryGetPiece(index, out var teamedPiece) && board.activePieces.ContainsKey(teamedPiece))
            {
                IPiece piece = board.activePieces[teamedPiece];

                // I don't THINK this is still needed
                // I may have added it for some case where the IPiece is being deleted but somehow not removed from the activePieces structure
                // Thus giving us a null object, but IPiece is an interface, so we can't test if that's null. 
                // We know IPiece is only added to MonoBehaviours, so we can safely cast
                if((MonoBehaviour)piece == null)
                    continue;

                piece.HighlightWithColor(piece.team == hoveredPiece.team ? greenColor : orangeColor);
            }
        }
    }

    private void ClearPiecesColorization(List<Index> set)
    {
        BoardState boardState = board.GetCurrentBoardState();
        foreach(Index index in set)
        {
            if(boardState.TryGetPiece(index, out var teamedPiece) && board.activePieces.ContainsKey(teamedPiece))
            {
                IPiece piece = board.activePieces[teamedPiece];
                
                // I don't THINK this is still needed
                // I may have added it for some case where the IPiece is being deleted but somehow not removed from the activePieces structure
                // Thus giving us a null object, but IPiece is an interface, so we can't test if that's null. 
                // We know IPiece is only added to MonoBehaviours, so we can safely cast
                if((MonoBehaviour)piece == null)
                    continue;
                
                piece.ResetHighlight();
            }
        }
    }

    public void EnablePreview()
    {
        foreach((Hex hex, MoveType moveType) in previewMoves)
        {
            hex.SetOutlineColor(hoverColor);
            hex.ToggleSelect();
        }
        
        ColorizePieces();
    }

    public void DisablePreview()
    {
        foreach ((Hex hex, MoveType moveType) in previewMoves)
            hex.ToggleSelect();
        previewMoves.Clear();

        ClearPiecesColorization(attacksConcerningHex);
    }

    public void LeftClick(CallbackContext context)
    {
        if(promotionDialogue != null && promotionDialogue.gameObject.activeSelf)
            return;
        
        BoardState currentBoardState = board.GetCurrentBoardState();
        if(context.started)
        {
            if(arrowTool.arrowsVisible)
                arrowTool.ClearArrows();
                
            if(historyPanel.panelPointer == historyPanel.currentTurnPointer)
                MouseDown(currentBoardState);
        }
        else if(context.canceled)
        {
            if(historyPanel.panelPointer != historyPanel.currentTurnPointer && selectedPiece != null)
                historyPanel.JumpToPresent();

            ReleaseMouse(currentBoardState);
        }
    }

    private void MouseDown(BoardState currentBoardState)
    {
        Team currentTurn = board.GetCurrentTurn();
        // Later allow players to queue a move, but for now, just prevent even clicking a piece when not their turn
        if(multiplayer != null && multiplayer.gameParams.localTeam != currentTurn)
            return;
        
        // The options panel covers the board, so we don't want to allow selecting a piece if the options panel is open
        if(optionsPanel != null && optionsPanel.visible)
            return;
        
        // If AI is enabled, return if it's the AI's turn
        if(aiController != null)
        {
            if(aiController.WhiteAIEnabled() && currentTurn == Team.White)
                return;
            if(aiController.BlackAIEnabled() && currentTurn == Team.Black)
                return;
        }

        // If the game is over, prevent any further moves
        if(board.currentGame.endType != GameEndType.Pending)
            return;

        bool cursorVisability = cursor != null ? cursor.visible : true;

        // When moveing pieces on the board, we determine what piece is being moved by the hex the player is hovering
        if(cursorVisability && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hexHit, 100, hexMask))
        {
            if(hexHit.collider == null)
                return;

            if(hexHit.collider.TryGetComponent<Hex>(out Hex clickedHex) && currentBoardState.allPiecePositions.ContainsKey(clickedHex.index) && clickedHex.isGameHex)
            {
                IPiece pieceOnHex = board.activePieces[currentBoardState.allPiecePositions[clickedHex.index]];
                if(pieceOnHex.team == currentBoardState.currentMove)
                {
                    Select(currentBoardState, pieceOnHex);
                    PlayPickupNoise();
                    return;
                }
            }
        }

        // But pulling pieces ouf of jail in free place mode doesn't have a hex for us to check, so let's cast a ray and check that instead.
        if(isFreeplaced)
        {
            // if(Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit pieceHit, 100, layerMask))
            if(cursorVisability && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit pieceHit, 100, layerMask))
            {
                if(pieceHit.collider == null)
                    return;
                
                if(pieceHit.collider.TryGetComponent<IPiece>(out IPiece clickedPiece) && clickedPiece.team == currentBoardState.currentMove && clickedPiece.captured)
                {
                    Select(currentBoardState, clickedPiece, true);
                    PlayPickupNoise();
                    return;
                }
            }
        }
    }

    private void ReleaseMouse(BoardState currentBoardState)
    {
        if(lastChangedPiece != null)
            ResetLastChangedRenderer();
        
        bool ignoreHexToggle = true;
        bool cursorVisability = cursor != null ? cursor.visible : true;

        if(cursorVisability && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, 100))
        {
            if(hit.collider.TryGetComponent<IPiece>(out IPiece hoveredPiece) && selectedPiece != null)
            {
                if(!hoveredPiece.captured)
                {
                    Hex otherPieceOccupiedHex = board.GetHexIfInBounds(hoveredPiece.location);
                    // Free place mode override
                    if(isFreeplaced)
                    {
                        if(selectedPiece == hoveredPiece)
                        {
                            BoardState currentState = board.GetCurrentBoardState();
                            lastMoveTracker.UpdateText(new Move(
                                board.currentGame.GetTurnCount(),
                                currentState.currentMove,
                                Piece.King,
                                Index.invalid,
                                Index.invalid
                            ));
                        }
                        if(!selectedPiece.captured)
                        {
                            if(selectedPiece.team == hoveredPiece.team && selectedPiece != hoveredPiece)
                                Defend(hoveredPiece);
                            else
                                MoveOrAttack(otherPieceOccupiedHex);
                        }
                    }
                    else
                    {
                        ignoreHexToggle = false;
                        if(pieceMoves.Contains((otherPieceOccupiedHex, MoveType.Attack)))
                            MoveOrAttack(otherPieceOccupiedHex);
                        else if(pieceMoves.Contains((otherPieceOccupiedHex, MoveType.Defend)))
                            Defend(hoveredPiece);
                        else if(pieceMoves.Contains((otherPieceOccupiedHex, MoveType.EnPassant)))
                            EnPassant(currentBoardState, otherPieceOccupiedHex);
                    }
                }
                // The piece was dropped on top of a piece in jail
                else if(isFreeplaced)
                {
                    if(!selectedPiece.captured && selectedPiece.piece != Piece.King)
                    {
                        board.Enprison(selectedPiece);
                        Move move = board.currentGame.GetLastMove(isFreeplaced);
                        lastMoveTracker.UpdateText(move);
                    }
                }
            }

            // Hovering on a hex
            if(hit.collider.TryGetComponent<Hex>(out Hex hitHex) && selectedPiece != null && hitHex.isGameHex)
            {
                IPiece otherPiece = currentBoardState.allPiecePositions.ContainsKey(hitHex.index) 
                    ? board.activePieces[currentBoardState.allPiecePositions[hitHex.index]] 
                    : null;

                // Free place mode override
                if(isFreeplaced)
                {
                    if(otherPiece == selectedPiece)
                    {
                        BoardState currentState = board.GetCurrentBoardState();
                        lastMoveTracker.UpdateText(new Move(
                            board.currentGame.GetTurnCount(),
                            currentState.currentMove,
                            Piece.King,
                            Index.invalid,
                            Index.invalid
                        ));
                    }
                    else if(otherPiece == null)
                    {
                        if(selectedPiece.captured)
                        {
                            Jail jail = GameObject.FindObjectsOfType<Jail>()
                                .Where(jail => jail.teamToPrison == selectedPiece.team)
                                .First();
                            jail.RemoveFromPrison(selectedPiece);
                        }
                        MoveOrAttack(hitHex);
                    }
                    else if(!selectedPiece.captured)
                    {
                        if(selectedPiece.team == otherPiece.team)
                            Defend(otherPiece);
                        else
                            MoveOrAttack(hitHex);
                    }
                }
                else
                {
                    ignoreHexToggle = false;
                    if(pieceMoves.Contains((hitHex, MoveType.Attack)) || pieceMoves.Contains((hitHex, MoveType.Move)))
                        MoveOrAttack(hitHex);
                    else if(pieceMoves.Contains((hitHex, MoveType.Defend)))
                        Defend(otherPiece);
                    else if(pieceMoves.Contains((hitHex, MoveType.EnPassant)))
                        EnPassant(currentBoardState, hitHex);
                }
            }

            if(isFreeplaced && selectedPiece != null)
            {
                if(!selectedPiece.captured && selectedPiece.piece != Piece.King)
                    ignoreHexToggle = false;
                // Piece dropped on top of jail
                if(hit.collider.TryGetComponent<Jail>(out Jail jail) && !selectedPiece.captured && selectedPiece.piece != Piece.King)
                {
                    board.Enprison(selectedPiece);
                    Move move = board.currentGame.GetLastMove(isFreeplaced);
                    lastMoveTracker.UpdateText(move);
                }
            }
            else if(!isFreeplaced)
            {
                // If dropping a piece on a jail and free place mode is not on, we need to be sure to toggle the hex that the selected piece is occupying to off
                if(hit.collider.TryGetComponent<Jail>(out Jail jail))
                    ignoreHexToggle = false;
            }
        }
        else
            ignoreHexToggle = false;

        if(selectedPiece != null)
        {
            PlayCancelNoise();
            DeselectPiece(selectedPiece.location, ignoreHexToggle);
        }
    }

    public void PlayCancelNoise() => audioSource.PlayOneShot(cancelNoise);
    public void PlayPickupNoise() => audioSource.PlayOneShot(pickupNoise);

    private void Select(BoardState currentBoardState, IPiece clickedPiece, bool fromJail = false)
    {
        if(selectedPiece != null)
            DeselectPiece(selectedPiece.location);

        cursor?.SetCursor(CursorType.Grab);

        // Select new piece and highlight all of the places it can move to on the current board state
        selectedPiece = clickedPiece;
        onMouse.PickUp(selectedPiece.obj);
        onMouse.SetBaseColor(selectedPiece.team == Team.White ? whiteColor : blackColor);
        
        if(!fromJail)
        {
            pieceMoves = board.currentGame.GetAllValidMovesForPiece((selectedPiece.team, selectedPiece.piece), currentBoardState)
                .Select(kvp => (board.GetHexIfInBounds(kvp.target), kvp.moveType))
                .ToList();
            
            // Highlight each possible move the correct color
            foreach((Hex hex, MoveType moveType) in pieceMoves)
            {
                hex.SetOutlineColor(moveTypeHighlightColors[(int)moveType]);
                hex.ToggleSelect();
            }

            Hex selectedHex = board.GetHexIfInBounds(selectedPiece.location);
            selectedHex.SetOutlineColor(selectedPieceColor);
            selectedHex.ToggleSelect();
        }
    }

    public void DeselectPiece(Index fromIndex, bool fromJail = false)
    {
        cursor?.SetCursor(CursorType.Default);

        if(selectedPiece == null)
            return;

        foreach((Hex hex, MoveType moveType) in pieceMoves)
            hex.ToggleSelect();
        pieceMoves.Clear();

        if(!fromJail)
            board.GetHexIfInBounds(fromIndex).ToggleSelect();

        onMouse.PutDown();
        hoverExitedInitialHex = false;
        lastHoveredHex = null;
        
        selectedPiece = null;
    }

    private void MoveOrAttack(Hex hitHex)
    {
        Index pieceStartLoc = selectedPiece.location;
        bool fromJail = false;

        if(!board.activePieces.ContainsKey((selectedPiece.team, selectedPiece.piece)))
        {
            fromJail = true;
            board.activePieces.Add((selectedPiece.team, selectedPiece.piece), selectedPiece);
        }

        if((selectedPiece is Pawn pawn) && pawn.GetGoalInRow(hitHex.index.row) == hitHex.index.row)
        {
            // We don't send a boardstate right now when multiplayer, as the promotion will finish that for us
            board.MovePieceForPromotion(selectedPiece, hitHex, board.GetCurrentBoardState());
            DeselectPiece(pieceStartLoc, fromJail);
            return;
        }
        else
        {
            BoardState newState = board.MovePiece(selectedPiece, hitHex.index, board.GetCurrentBoardState());
            if(multiplayer != null)
                multiplayer.SendBoard(newState);
            board.AdvanceTurn(newState);
            DeselectPiece(pieceStartLoc, fromJail);
        }
    }

    private void Defend(IPiece pieceToDefend)
    {
        Index startLoc = selectedPiece.location;
        bool fromJail = false;
        if(!board.activePieces.ContainsKey((selectedPiece.team, selectedPiece.piece)))
        {
            fromJail = true;
            board.activePieces.Add((selectedPiece.team, selectedPiece.piece), selectedPiece);
        }

        BoardState newState = board.Swap(selectedPiece, pieceToDefend, board.GetCurrentBoardState());
        
        if(multiplayer != null)
            multiplayer.SendBoard(newState);

        board.AdvanceTurn(newState);
        DeselectPiece(startLoc, fromJail);
    }

    private void EnPassant(BoardState currentBoardState, Hex hitHex)
    {
        Index startLoc = selectedPiece.location;
        Index enemyLoc = HexGrid.GetNeighborAt(hitHex.index, currentBoardState.currentMove == Team.White ? HexNeighborDirection.Down : HexNeighborDirection.Up).Value;
        (Team enemyTeam, Piece enemyType) = currentBoardState.allPiecePositions[enemyLoc];
        BoardState newState = board.EnPassant((Pawn)selectedPiece, enemyTeam, enemyType, hitHex.index, currentBoardState);

        if(multiplayer != null)
            multiplayer.SendBoard(newState);

        board.AdvanceTurn(newState);
        DeselectPiece(startLoc);
    }

    private int GetGoal(Team team, int row) => team == Team.White ? 18 - (row % 2) : row % 2;
}