using System;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using Extensions;

public class AIBattleController : MonoBehaviour
{
    public bool debugControls = false;
    public float MinimumTurnTimeSec = 1f;
    private Board board;
    private IHexAI whiteAI;
    private IHexAI blackAI;

    Team currentMoveFor = Team.None;
    public int selectedWhiteAI {get; private set;}
    public int selectedBlackAI {get; private set;}
    bool isGameRunning;
    bool needsReset = false;

    float nextMoveTime;
    float moveRequestedAt;
    Team moveRequestedFor;
    Task<HexAIMove> pendingMove = null;

    private (string name, Func<IHexAI> factory)[] AIOptions = Array.Empty<(string, Func<IHexAI>)>();
    private string[] AINames = Array.Empty<string>();

    private void Awake()
    {
        AIOptions = new (string, Func<IHexAI>)[] {
            ("None", () => null),
            ("Clueless", () => new RandomAI()),
            ("Bloodthirsty", () => new BloodthirstyAI()),
            ("Terite (depth 1)", () => new TeriteAI(1)),
            ("Terite (depth 2)", () => new TeriteAI(2)),
            ("Terite (depth 3)", () => new TeriteAI(3)),
            ("Terite (depth 4)", () => new TeriteAI(4)),
            ("Terite (depth 5)", () => new TeriteAI(5)),
            ("Terite (depth 6)", () => new TeriteAI(6))
        };
        AINames = AIOptions.Select(ai => ai.name).ToArray();
    }

    private void Start()
    {
        board = GetComponent<Board>();
        board.newTurn += HandleNewTurn;
        currentMoveFor = board.GetCurrentTurn();
    }

    void OnGUI()
    {
        if(!debugControls)
            return;
            
        GUI.enabled = !needsReset;
        GUILayout.BeginHorizontal();
        GUILayout.Label("White: ");
        selectedWhiteAI = GUILayout.Toolbar(selectedWhiteAI, AINames);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Black: ");
        selectedBlackAI = GUILayout.Toolbar(selectedBlackAI, AINames);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Start Game"))
        {
            StartGame();
        }

        GUI.enabled = true;
        if (GUILayout.Button("Reset"))
        {
            board.Reset();
        }

        if (isGameRunning)
        {
            if (whiteAI != null)
            {
                GUILayout.BeginHorizontal();
                var whiteLines = whiteAI.GetDiagnosticInfo();
                if (whiteLines != null)
                    foreach (var line in whiteLines)
                        GUILayout.Label(line);
                GUILayout.EndHorizontal();
            }
            if (blackAI != null)
            {
                GUILayout.BeginHorizontal();
                var blackLines = blackAI.GetDiagnosticInfo();
                if (blackLines != null)
                    foreach (var line in blackLines)
                        GUILayout.Label(line);
                GUILayout.EndHorizontal();
            }

        }
    }

    public void StartGame()
    {
        whiteAI = AIOptions[selectedWhiteAI].factory();
        blackAI = AIOptions[selectedBlackAI].factory();
        Debug.Log($"Starting Game! White: {whiteAI}, Black: {blackAI}");
        isGameRunning = true;
        needsReset = true;
    }

    public void SetAI(Team team, int aiID)
    {
        if(team == Team.None)
            return;
        else if(team == Team.White)
            selectedWhiteAI = aiID;
        else
            selectedBlackAI = aiID;
    }

    void Update()
    {
        if (!isGameRunning)
            return;

        if (board.currentGame.winner != Winner.Pending)
            return;

        if (pendingMove != null)
        {
            if (!pendingMove.IsCompleted)
                return;

            if (Time.timeSinceLevelLoad < nextMoveTime)
                return;

            var move = pendingMove.Result;
            pendingMove = null;

            if (moveRequestedFor != currentMoveFor)
            {
                Debug.LogError($"Got move for {moveRequestedFor}, but it's {currentMoveFor} turn to move");
                return;
            }

            ApplyMove(move);
            return;
        }

        if (currentMoveFor != Team.None)
        {
            var ai = (currentMoveFor == Team.White) ? whiteAI : blackAI;
            if (ai == null)
                return;

            moveRequestedAt = Time.timeSinceLevelLoad;
            moveRequestedFor = currentMoveFor;
            pendingMove = ai.GetMove(board.currentGame);
        }
    }

    void HandleNewTurn(BoardState newState)
    {
        currentMoveFor = newState.currentMove;
        nextMoveTime = Time.timeSinceLevelLoad + MinimumTurnTimeSec;

        if (currentMoveFor == Team.None)
        {
            Debug.Log("Game Completed!");
            isGameRunning = false;
            return;
        }

        if (pendingMove != null)
        {
            Debug.LogWarning($"Clearning pending move for {moveRequestedFor}");
            pendingMove = null;
            if (moveRequestedFor == Team.White && whiteAI != null)
                whiteAI.CancelMove();
            else if (moveRequestedFor == Team.Black && blackAI != null)
                blackAI.CancelMove();
        }
    }

    #region Move application
    void ApplyMove(HexAIMove move)
    {
        var bs = board.GetCurrentBoardState();
        IPiece piece = board.activePieces[bs.allPiecePositions[move.start]];

        switch (move.moveType)
        {
            case MoveType.Move:
            case MoveType.Attack:
                ApplyMoveOrAttack(piece, move);
                break;

            case MoveType.Defend:
                ApplyDefend(piece, move);
                break;

            case MoveType.EnPassant:
                ApplyEnPassant(piece, move);
                break;

            case MoveType.None:
            default:
                Debug.LogError("AI returned no move!!!");
                break;
        }
    }

    private void ApplyMoveOrAttack(IPiece piece, HexAIMove move)
    {
        BoardState newState;
        if((piece is Pawn pawn) && !move.promoteTo.IsPawn())
        {
            board.currentGame.AddPromotion(new Promotion(pawn.team, pawn.piece, move.promoteTo, board.currentGame.GetTurnCount()));
            board.PromoteIPiece(pawn, move.promoteTo);
            newState = board.MovePiece(piece, move.target, board.GetCurrentBoardState());
        }
        else
        {
            newState = board.MovePiece(piece, move.target, board.GetCurrentBoardState());
        }
        board.AdvanceTurn(newState);
    }

    private void ApplyEnPassant(IPiece piece, HexAIMove move)
    {
        var state = board.GetCurrentBoardState();
        Index enemyLoc = move.target.GetNeighborAt(piece.team == Team.White ? HexNeighborDirection.Down : HexNeighborDirection.Up)!.Value;
        (Team enemyTeam, Piece enemyType) = state.allPiecePositions[enemyLoc];

        BoardState newState = board.EnPassant((Pawn)piece, enemyTeam, enemyType, move.target, state);
        board.AdvanceTurn(newState);
    }

    private void ApplyDefend(IPiece piece, HexAIMove move)
    {
        foreach (var kvp in board.activePieces)
        {
            if (kvp.Value.location == move.target)
            {
                BoardState newState = board.Swap(piece, kvp.Value, board.GetCurrentBoardState());
                board.AdvanceTurn(newState);
                return;
            }
        }

        Debug.LogError($"No piece to defend at {move.target} :(");
        return;
    }
    #endregion

    private void OnDestroy()
    {
        if (whiteAI != null)
            whiteAI.CancelMove();
        if (blackAI != null)
            blackAI.CancelMove();
    }
}
