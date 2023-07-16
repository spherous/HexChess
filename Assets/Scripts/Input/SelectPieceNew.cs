using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using System.Linq;
using Sirenix.OdinInspector;
using Extensions;

public class SelectPieceNew : MonoBehaviour
{
    MouseData mouseData;
    Multiplayer multiplayer;
    [SerializeField] SmoothHalfOrbitalCamera smoothHalfOrbitalCamera;
    [SerializeField] private Board board;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private PromotionDialogue promotionDialogue;
    [SerializeField] private TurnHistoryPanel historyPanel;
    [SerializeField] private GroupFader optionsPanel;
    public AudioClip cancelNoise;
    public AudioClip pickupNoise;
    public IPiece selectedPiece {get; private set;}
    [SerializeField] private OnMouse onMouse;
    private FreePlaceModeToggle freePlaceMode;
    [SerializeField] private LastMoveTracker lastMoveTracker;
    bool isFreeplaced => !multiplayer && freePlaceMode.toggle.isOn;
    List<(Hex, MoveType)> pieceMoves = new List<(Hex, MoveType)>();
    List<(Hex, MoveType)> previewMoves = new List<(Hex, MoveType)>();
    public List<Color> moveTypeHighlightColors = new List<Color>();
    public Color selectedColor;
    public Color greenColor;
    public Color redColor;
    public Color orangeColor;
    public Color whiteColor;
    public Color blackColor;
    [ShowInInspector, ReadOnly] List<Index> attacksConcerningHex = new List<Index>();
    Dictionary<IPiece, List<Index>> attacksConcerningHexDict = new Dictionary<IPiece, List<Index>>();
    IPiece lastChangedPiece;

    private VirtualCursor cursor;

    private void Awake() 
    {
        multiplayer = GameObject.FindObjectOfType<Multiplayer>();
        cursor = GameObject.FindObjectOfType<VirtualCursor>();
        freePlaceMode = GameObject.FindObjectOfType<FreePlaceModeToggle>();
        mouseData = GameObject.FindObjectOfType<MouseData>();

        mouseData.onHoverHex += OnHoverHex;
        mouseData.onHoverIPiece += OnHoverIPiece;

        board.newTurn += NewTurn;
    }

    private void OnHoverIPiece(IPiece hoveredIPiece) => InformationalOverlay(hoveredIPiece);
    private void OnHoverHex(Hex hoveredHex) => ColorizePieceBasedOnMoves(hoveredHex);

    private void OnDestroy()
    {
        if(mouseData != null)
        {
            mouseData.onHoverHex -= OnHoverHex;
            mouseData.onHoverIPiece -= OnHoverIPiece;
        }

        if(board != null)
            board.newTurn -= NewTurn;
    }

    private void NewTurn(BoardState newState) => attacksConcerningHexDict.Clear();

    private void ColorizePieceBasedOnMoves(Hex hoveredHex)
    {
        if(smoothHalfOrbitalCamera != null && smoothHalfOrbitalCamera.freeLooking)
            return;
        else if(optionsPanel != null && optionsPanel.visible)
            return;
        else if(onMouse == null)
            return;
        else if(selectedPiece == null)
            return;

        SetOnMouseColor(hoveredHex);            

        if(hoveredHex == null)
            return;

        if(board.TryGetIPieceFromIndex(hoveredHex.index, out IPiece hoveredPiece) && hoveredPiece != selectedPiece)
        {
            if(hoveredPiece == lastChangedPiece)
                return;

            if(lastChangedPiece != null)
                ResetLastChangedRenderer();

            Color toSet = hoveredPiece.team == Team.White ? whiteColor : blackColor;

            // colorize for attack
            if(pieceMoves.Contains((hoveredHex, MoveType.Attack)) || pieceMoves.Contains((hoveredHex, MoveType.EnPassant)))
                toSet = redColor;
            // colorize for defend or move
            else if(pieceMoves.Contains((hoveredHex, MoveType.Defend)) || pieceMoves.Contains((hoveredHex, MoveType.Move)))
                toSet = greenColor;

            hoveredPiece.HighlightWithColor(toSet);
            lastChangedPiece = hoveredPiece;
        }
        else if(lastChangedPiece != null)
            ResetLastChangedRenderer();
    }

    private void SetOnMouseColor(Hex hex = null)
    {
        if(hex == null)
        {
            onMouse.SetColor(redColor);
            return;
        }

        if(pieceMoves.Contains((hex, MoveType.Attack))
            || pieceMoves.Contains((hex, MoveType.Move))
            || pieceMoves.Contains((hex, MoveType.Defend))
            || pieceMoves.Contains((hex, MoveType.EnPassant))
        )
            onMouse.SetColor(greenColor);
        else if(selectedPiece.location == hex.index)
            onMouse.SetColor(selectedPiece.team == Team.White ? whiteColor : blackColor);
        else
            onMouse.SetColor(redColor);
    }

    private void ResetLastChangedRenderer()
    {
        lastChangedPiece?.ResetHighlight();
        lastChangedPiece = null;
    }

    private void InformationalOverlay(IPiece hoveredIPiece)
    {
        if(multiplayer != null && !multiplayer.gameParams.showMovePreviews)
            return;
        else if(!PlayerPrefs.GetInt("HandicapOverlay", true.BoolToInt()).IntToBool())
            return;
        else if(onMouse != null && onMouse.isPickedUp)
            return;
        else if(promotionDialogue != null && promotionDialogue.gameObject.activeSelf)
            return;
        else if(smoothHalfOrbitalCamera != null && smoothHalfOrbitalCamera.freeLooking)
        {
            // This should probably happen as soon as freeLooking is true, rather than waiting until the player starts moving the mouse while free looking
            if(previewMoves.Count > 0)
                DisablePreview();
            return;
        }
        else if(optionsPanel != null && optionsPanel.visible)
            return;

        if(hoveredIPiece == null && previewMoves.Count > 0)
            DisablePreview();
        else if(hoveredIPiece != null)
        {
            DisablePreview();

            BoardState currentBoardState = board.GetCurrentBoardState();
            Hex hexPieceIsOn = board.GetHexIfInBounds(hoveredIPiece.location);

            var incomingPreviewMoves = board.currentGame.GetAllValidMovesForPiece(
                (hoveredIPiece.team, hoveredIPiece.piece),
                currentBoardState,
                true
            ).Select(kvp => (board.GetHexIfInBounds(kvp.target), kvp.moveType));

            previewMoves = incomingPreviewMoves.ToList();
            previewMoves.Add((hexPieceIsOn, MoveType.None)); // Include the piece's current hex in the preview

            List<Index> validAttacksOnHex = board.currentGame.GetValidAttacksConcerningHex(hoveredIPiece.location, currentBoardState).ToList();

            if(!attacksConcerningHexDict.ContainsKey(hoveredIPiece))
                attacksConcerningHexDict.Add(hoveredIPiece, validAttacksOnHex);
            else if(attacksConcerningHexDict[hoveredIPiece] != validAttacksOnHex)
            {
                attacksConcerningHexDict.Remove(hoveredIPiece);
                attacksConcerningHexDict.Add(hoveredIPiece, validAttacksOnHex);
            }

            EnablePreview();
            ColorizePiecesInRelationTo(hoveredIPiece);
        }
    }

    private void ColorizePiecesInRelationTo(IPiece hoveredPiece)
    {
        BoardState boardState = board.GetCurrentBoardState();
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
            hex?.SetOutlineColor(selectedColor);
            hex?.ToggleSelect();
        }
    }

    public void DisablePreview()
    {
        foreach((Hex hex, MoveType moveType) in previewMoves)
            hex?.ToggleSelect();
        previewMoves.Clear();

        ClearPiecesColorization(attacksConcerningHex);
    }

    public void LeftClick(CallbackContext context)
    {
        if(promotionDialogue != null && promotionDialogue.gameObject.activeSelf)
            return;
        
        BoardState currentBoardState = board.GetCurrentBoardState();
        if(context.started)
            MouseDown(currentBoardState);
        else if(context.canceled)
        {
            if(!historyPanel.isShowingCurrentTurn && selectedPiece != null)
                historyPanel.JumpToPresent();

            ReleaseMouse(currentBoardState);
        }
    }

    private void MouseDown(BoardState currentBoardState)
    {
        if(board == null)
            return;
        // When adding premoving, this will change.
        else if(!board.IsPlayerTurn())
            return;
        // The options panel covers the board, so we don't want to allow selecting a piece if the options panel is open
        else if(optionsPanel != null && optionsPanel.visible)
            return;
        // If the game is over, prevent any further moves
        else if(board.currentGame.endType != GameEndType.Pending)
            return;
        // If we've viewing a move in the past, don't allow the player to move the piece,
        // This will change when adding lines
        else if(!historyPanel.isShowingCurrentTurn)
            return;

        if(mouseData.hoveredIPiece != null && mouseData.hoveredIPiece.team == currentBoardState.currentMove)
        {
            Select(currentBoardState, mouseData.hoveredIPiece);
            PlayPickupNoise();
            return;
        }

        // But pulling pieces ouf of jail in free place mode doesn't have a hex for us to check, so let's cast a ray and check that instead.
        // if(isFreeplaced)
        // {
        //     bool cursorVisability = cursor != null ? cursor.visible : true;
        //     if(cursorVisability && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit pieceHit, 100, layerMask))
        //     {
        //         if(pieceHit.collider == null)
        //             return;
                
        //         if(pieceHit.collider.TryGetComponent<IPiece>(out IPiece clickedPiece) && clickedPiece.team == currentBoardState.currentMove && clickedPiece.captured)
        //         {
        //             Select(currentBoardState, clickedPiece, true);
        //             PlayPickupNoise();
        //             return;
        //         }
        //     }
        // }
    }

    private void ReleaseMouse(BoardState currentBoardState)
    {
        if(lastChangedPiece != null)
            ResetLastChangedRenderer();
        
        bool ignoreHexToggle = true;
        IPiece otherPiece = null;

        if(mouseData.hoveredHex != null && selectedPiece != null && board.TryGetIPieceFromIndex(mouseData.hoveredHex.index, out otherPiece))
        {
            if(isFreeplaced)
                FreePlaceExecuteMove(currentBoardState, otherPiece);
            else
            {
                ignoreHexToggle = false;
                ExecuteMove(currentBoardState, otherPiece);
            }
        }
        else
        {
            ignoreHexToggle = false;
            if(mouseData.hoveredHex != null)
                ExecuteMove(currentBoardState, otherPiece);
        }


        if(selectedPiece != null)
        {
            if(isFreeplaced)
            {
                if(!selectedPiece.captured && selectedPiece.piece != Piece.King)
                    ignoreHexToggle = false;
                // Piece dropped on top of jail
                // if(hit.collider.TryGetComponent<Jail>(out Jail jail) && !selectedPiece.captured && selectedPiece.piece != Piece.King)
                // {
                //     board.Enprison(selectedPiece);
                //     Move move = board.currentGame.GetLastMove(isFreeplaced);
                //     lastMoveTracker.UpdateText(move);
                // }
            }

            PlayCancelNoise();
            DeselectPiece(selectedPiece.location, ignoreHexToggle);
        }
    }

    private void FreePlaceExecuteMove(BoardState currentBoardState, IPiece otherPiece)
    {
        // dropping on self to skip the move
        if(otherPiece == selectedPiece)
        {
            BoardState currentState = board.GetCurrentBoardState();
            currentState.currentMove = currentState.currentMove.Enemy();
            lastMoveTracker.UpdateText(new Move(
                board.currentGame.GetTurnCount(),
                currentState.currentMove,
                Piece.King,
                Index.invalid,
                Index.invalid
            ));
            board.AdvanceTurn(currentState);
        }
        else if(otherPiece == null)
        {
            // pulling out of jail
            if(selectedPiece.captured)
            {
                Jail jail = GameObject.FindObjectsOfType<Jail>()
                    .Where(jail => jail.teamToPrison == selectedPiece.team)
                    .First();
                jail.RemoveFromPrison(selectedPiece);
            }
            MoveOrAttack(mouseData.hoveredHex);
        }
        else if(!selectedPiece.captured) // valid move
            ExecuteMove(currentBoardState, otherPiece);
    }

    private void ExecuteMove(BoardState currentBoardState, IPiece otherPiece)
    {
        if(isFreeplaced)
        {
            if(otherPiece != null && selectedPiece.team == otherPiece.team)
                Defend(otherPiece);
            else
                MoveOrAttack(mouseData.hoveredHex);

            return;
        }

        if(pieceMoves.Contains((mouseData.hoveredHex, MoveType.Attack)) || pieceMoves.Contains((mouseData.hoveredHex, MoveType.Move)))
            MoveOrAttack(mouseData.hoveredHex);
        else if(pieceMoves.Contains((mouseData.hoveredHex, MoveType.Defend)))
            Defend(otherPiece);
        else if(pieceMoves.Contains((mouseData.hoveredHex, MoveType.EnPassant)))
            EnPassant(currentBoardState, mouseData.hoveredHex);
    }

    public void PlayCancelNoise() => audioSource.PlayOneShot(cancelNoise);
    public void PlayPickupNoise() => audioSource.PlayOneShot(pickupNoise);

    private void Select(BoardState currentBoardState, IPiece clickedPiece, bool fromJail = false)
    {
        DisablePreview();

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
            selectedHex.SetOutlineColor(selectedColor);
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
        
        selectedPiece = null;
    }

    private void MoveOrAttack(Hex hitHex)
    {
        if(selectedPiece == null)
            return;

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
        if(selectedPiece == null)
            return;

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
        if(selectedPiece == null)
            return;

        Index startLoc = selectedPiece.location;
        Index enemyLoc = HexGrid.GetNeighborAt(hitHex.index, currentBoardState.currentMove == Team.White ? HexNeighborDirection.Down : HexNeighborDirection.Up).Value;
        (Team enemyTeam, Piece enemyType) = currentBoardState.allPiecePositions[enemyLoc];
        BoardState newState = board.EnPassant((Pawn)selectedPiece, enemyTeam, enemyType, hitHex.index, currentBoardState);

        if(multiplayer != null)
            multiplayer.SendBoard(newState);

        board.AdvanceTurn(newState);
        DeselectPiece(startLoc);
    }
}