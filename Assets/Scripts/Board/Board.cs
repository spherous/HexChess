using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using Extensions;
using System.Collections;

public class Board : SerializedMonoBehaviour
{
    [SerializeField] private PromotionDialogue promotionDialogue;
    [SerializeField] private LastMoveTracker moveTracker;
    [SerializeField] private TurnPanel turnPanel;
    [SerializeField] private Timers timers;
    [SerializeField] private SmoothHalfOrbitalCamera cam;
    [SerializeField] private FreePlaceModeToggle freePlaceMode;
    bool isFreeplaced => freePlaceMode != null && freePlaceMode.toggle.isOn;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TurnHistoryPanel turnHistoryPanel;
    public AudioClip moveClip;
    public AudioClip defendClip;
    public List<AudioClip> captureClips = new List<AudioClip>();
    public AudioClip winFanfare;
    public AudioClip loseKnell;
    public AudioClip drawAudio;
    public List<Jail> jails = new List<Jail>();
    [SerializeField] private GameObject hexPrefab;
    public Dictionary<(Team, Piece), GameObject> piecePrefabs = new Dictionary<(Team, Piece), GameObject>();
    [ReadOnly, ShowInInspector, DisableInEditorMode, HideIf("@currentGame == null")] public IEnumerable<BoardState> turnHistory => currentGame?.turnHistory;
    [ReadOnly] public Dictionary<(Team, Piece), IPiece> activePieces = new Dictionary<(Team, Piece), IPiece>();
    public delegate void NewTurn(BoardState newState);
    [HideInInspector] public NewTurn newTurn;
    public delegate void GameOver(Game game);
    [HideInInspector] public GameOver gameOver;
    [SerializeField] public HexGrid hexGrid;
    [OdinSerialize] public List<List<Hex>> hexes = new List<List<Hex>>();
    List<Hex> highlightedHexes = new List<Hex>();
    [ReadOnly] public readonly string defaultBoardStateFileLoc = "DefaultBoardState";
    public List<string> gamesToLoadLoc = new List<string>();
    [ReadOnly, ShowInInspector, DisableInEditorMode, HideIf("@currentGame == null")] public List<Promotion> promotions => currentGame?.promotions;
    public Color lastMoveHighlightColor;
    public bool surpressVictoryAudio = false;

    private BoardState? lastSetState = null;
    public Game currentGame {get; private set;}
    
    object lockObj = new object();
    bool hexGameOver = false;

    private void Awake() => ResetPieces(Game.CreateNewGame());
    private void Start() => newTurn?.Invoke(currentGame.GetCurrentBoardState());
    private void Update()
    {
        lock(lockObj)
        {
            if(hexGameOver)
            {
                hexGameOver = false;
                ProcessEndGame();
            }
        }
    }

    public void SetBoardState(BoardState newState, int? turn = null)
    {
        foreach(KeyValuePair<(Team team, Piece piece), GameObject> prefab in piecePrefabs)
        {
            IPiece piece;
            Jail applicableJail = jails[(int)prefab.Key.team];
            IPiece jailedPiece = applicableJail.GetPieceIfInJail(prefab.Key.piece);
            var prefabTeamedPiece = prefab.Key;
            GameObject prefabGO = prefab.Value;

            BoardState.defaultState.TryGetIndex(prefab.Key, out Index startLoc);
            Vector3 loc = GetHexIfInBounds(startLoc.row, startLoc.col).transform.position + Vector3.up;

            if(activePieces.ContainsKey(prefab.Key))
            {
                piece = activePieces[prefab.Key];
                // Reset promoted pawn if needed
                if(prefab.Key.piece.IsPawn() && !(piece is Pawn))
                    CheckForAndDemoteIPieceIfNeeded(turn, ref piece, prefabTeamedPiece, prefabGO, startLoc, loc);
            }
            else if(jailedPiece != null)
            {
                piece = jailedPiece;
                applicableJail.RemoveFromPrison(piece);
                // a piece coming out of jail needs to be added back into the Active Pieces dictionary
                activePieces.Add(prefab.Key, piece);
                
                // This IPiece might be promoted, if it is, and is moving out of jail due to crawling history to a point where it wasn't promoted, then the IPiece must be demoted to a pawn
                if(prefab.Key.piece.IsPawn() && !(piece is Pawn))
                    CheckForAndDemoteIPieceIfNeeded(turn, ref piece, prefabTeamedPiece, prefabGO, startLoc, loc);
            }
            else
            {
                piece = Instantiate(prefab.Value, loc, Quaternion.identity).GetComponent<IPiece>();
                piece.Init(prefab.Key.team, prefab.Key.piece, startLoc);
                activePieces.Add(prefab.Key, piece);
            }
            
            // It might need to be promoted.
            // Do that before moving to avoid opening the promotion dialogue when the pawn is moved to the promotion position
            piece = GetPromotedPieceIfNeeded(piece, turn);
            
            // If the piece is on the board, place it at the correct location
            if(newState.TryGetIndex(prefab.Key, out Index newLoc))
            {
                if(lastSetState.HasValue 
                    && lastSetState.Value.TryGetPiece(newLoc, out (Team team, Piece piece) teamedPiece) 
                    && teamedPiece != prefab.Key 
                    && activePieces.ContainsKey(teamedPiece)
                ){
                    IPiece occupyingPiece = activePieces[teamedPiece];
                    if(newState.TryGetIndex((occupyingPiece.team, occupyingPiece.piece), out Index belongsAtLoc) && belongsAtLoc == newLoc)
                        Enprison(occupyingPiece, false);
                }
                piece.MoveTo(GetHexIfInBounds(newLoc.row, newLoc.col));
                continue;
            }
            // Put the piece in the correct jail
            else
            {
                applicableJail.Enprison(piece);
                activePieces.Remove(prefab.Key);
            }
        }

        if(newState.currentMove != Team.None && currentGame.turnHistory.Count > 1)
        {
            Move newMove = currentGame.GetLastMove(isFreeplaced);
            if(newMove.lastTeam != Team.None)
                HighlightMove(newMove);
            else
                ClearMoveHighlight();
        }
        else
            ClearMoveHighlight();

        lastSetState = newState; 
    }

    private void CheckForAndDemoteIPieceIfNeeded(int? turn, ref IPiece piece, (Team team, Piece piece) prefabTeamedPiece, GameObject prefabGO, Index startLoc, Vector3 loc)
    {
        if(turn.HasValue)
        {
            Team team = piece.team;
            if(currentGame.TryGetApplicablePromo((team, piece.piece), turn.Value, out Promotion promotion))
            {
                GameObject properPromotedPrefab = piecePrefabs[(promotion.team, promotion.to)];
                if(properPromotedPrefab.GetComponent<IPiece>().GetType() != piece.GetType())
                {
                    Debug.Log("Pawn is promoted to the wrong piece.");
                    piece = ResetPieceToPrefab(startLoc, loc, prefabTeamedPiece, prefabGO);
                }
            }
            else
            {
                Debug.Log("No applicable promo found, resetting promoted piece to pawn.");
                Vector3 pos = piece.obj.transform.position;
                piece = ResetPieceToPrefab(startLoc, loc, prefabTeamedPiece, prefabGO);
                piece.obj.transform.position = pos;
            }
        }
        else
        {
            Debug.Log("No turn provided to check promo status, revert to pawn.");
            piece = ResetPieceToPrefab(startLoc, loc, prefabTeamedPiece, prefabGO);
        }
    }

    private IPiece ResetPieceToPrefab(Index startLoc, Vector3 loc, (Team team, Piece piece) teamedPiece, GameObject prefabGO)
    {
        IPiece piece;
        IPiece old = activePieces[teamedPiece];
        activePieces.Remove(teamedPiece);
        Destroy(old.obj);
        piece = Instantiate(prefabGO, loc, Quaternion.identity).GetComponent<IPiece>();
        piece.Init(teamedPiece.team, teamedPiece.piece, startLoc);
        activePieces.Add(teamedPiece, piece);
        return piece;
    }

    public void LoadGame(Game toLoad)
    {
        if(currentGame != null)
        {
            if(currentGame.onGameOver != null)
                currentGame.onGameOver -= HexChessGameEnded;
            
            currentGame.KillGame();
        }
        
        currentGame = toLoad;
        currentGame.onGameOver += HexChessGameEnded;

        foreach(Jail jail in jails)
            jail?.Clear();

        BoardState state = currentGame.GetCurrentBoardState();

        SetBoardState(state);
        turnHistoryPanel?.SetGame(currentGame);

        Move move = currentGame.GetLastMove(isFreeplaced);
        if(move.lastTeam != Team.None)
            moveTracker.UpdateText(move);

        // game.endType may not exist in older game saves, this bit of code supports both new and old save styles
        if(currentGame.endType != GameEndType.Pending)
        {
            IEnumerable<BoardState> lastMoveTurns = currentGame.turnHistory.Skip(currentGame.turnHistory.Count - 3).Take(2);

            // This creates a bug in the turn history where the ended game state is being added to the history panel and overriding a previous panel
            // Though it may be being utilized to update something else
            // newTurn?.Invoke(lastMoveTurns.Last()); 

            move = HexachessagonEngine.GetLastMove(lastMoveTurns.ToList(), currentGame.promotions, isFreeplaced);
            turnHistoryPanel.UpdateMovePanels(lastMoveTurns.Last(), move, Mathf.FloorToInt((float)currentGame.turnHistory.Count / 2f) + currentGame.turnHistory.Count % 2);
            moveTracker.UpdateText(move);
            HighlightMove(move);
            
            gameOver?.Invoke(currentGame);
        }
        else
        {
            if(currentGame.winner == Winner.Pending)
            {
                newTurn?.Invoke(state);

                if(PlayerPrefs.GetInt("AutoFlipCam", 1).IntToBool())
                    cam.SetToTeam(state.currentMove);
            }
            else
                gameOver?.Invoke(currentGame);
        }

        if(timers != null)
        {
            if(currentGame.timerDuration <= 0)
            {
                timers.gameObject.SetActive(currentGame.hasClock);
                timers.isClock = currentGame.hasClock;
            }
            else
            {
                timers.gameObject.SetActive(true);
                timers.SetTimers(currentGame.timerDuration);
            }
        }
    }

    public Game GetGame(string loc) 
    {
        #if UNITY_EDITOR
        AssetDatabase.Refresh();
        #endif

        return Game.Deserialize(((TextAsset)Resources.Load(loc, typeof(TextAsset))).text);        
    }

    public Team GetCurrentTurn()
    {
        if(promotionDialogue.gameObject.activeSelf)
            return Team.None;

        return currentGame.GetCurrentTurn();
    }
 
    public BoardState GetCurrentBoardState() => turnHistoryPanel != null && turnHistoryPanel.TryGetCurrentBoardState(out BoardState state) 
        ? state 
        : currentGame.GetCurrentBoardState();

    public void AdvanceTurn(BoardState newState, bool updateTime = true, bool surpressAudio = false)
    {
        currentGame.AdvanceTurn(newState, isFreeplaced, updateTime);
        newState = currentGame.GetCurrentBoardState();
        Multiplayer multiplayer = GameObject.FindObjectOfType<Multiplayer>();

        Move move = currentGame.GetLastMove(isFreeplaced);
        HighlightMove(move);

        if(!surpressAudio)
        {
            if(move.defendedPiece.HasValue)
                audioSource.PlayOneShot(defendClip);
            else if(move.capturedPiece.HasValue)
            {
                audioSource.PlayOneShot(moveClip);
                // If the last team to play was this player, use a high pitch for captures, else use a low pitch. If it's single player, randomize it.
                AudioClip captureClip = multiplayer ? multiplayer.gameParams.localTeam == move.lastTeam ? captureClips[0] : captureClips[1] : captureClips.ChooseRandom();
                StartCoroutine(PlayAudioAfterDelay(captureClip, 0.133f));
            }
            else
                audioSource.PlayOneShot(moveClip);
        }

        newTurn?.Invoke(newState);

        // In sandbox mode, flip the camera when the turn passes if the toggle is on
        if(multiplayer == null)
        {
            if(PlayerPrefs.GetInt("AutoFlipCam", 1).IntToBool())
                cam.SetToTeam(newState.currentMove);
        }
    }

    IEnumerator PlayAudioAfterDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.PlayOneShot(clip);
    }

    void ProcessEndGame()
    {
        Multiplayer multiplayer = GameObject.FindObjectOfType<Multiplayer>();
        BoardState finalState = currentGame.GetCurrentBoardState();

        switch(currentGame.endType)
        {
            case GameEndType.Checkmate:
                if(multiplayer && multiplayer.gameParams.localTeam == finalState.checkmate)
                {
                    multiplayer.SendGameEnd(finalState.executedAtTime, MessageType.Checkmate);
                    EndGame(currentGame.CurrentTime, currentGame.endType, currentGame.winner);
                }
                break;
            case GameEndType.Draw:
                if(multiplayer)
                    EndGame(currentGame.CurrentTime, currentGame.endType, currentGame.winner);
                break;
            case GameEndType.Flagfall:
                Team flagfellTeam = currentGame.winner == Winner.White ? Team.Black : Team.White;
                if(multiplayer && multiplayer.localTeam == flagfellTeam) // Only the team that flagfell sends the flagfall, this prevents both sending and both clients ending the game twice
                {
                    multiplayer.SendFlagfall(new Flagfall(flagfellTeam, finalState.executedAtTime));
                    EndGame(currentGame.CurrentTime, currentGame.endType, currentGame.winner);
                }                    
                break;
            case GameEndType.Stalemate:
                Team otherTeam = finalState.currentMove.Enemy();
                if(multiplayer && multiplayer.gameParams.localTeam == otherTeam) // this is probably broken
                {
                    multiplayer.SendGameEnd(finalState.executedAtTime, MessageType.Stalemate);
                    EndGame(currentGame.CurrentTime, currentGame.endType, currentGame.winner);
                }
                break;
            case GameEndType.Surrender:
                if(multiplayer) // The Surrender button is already handling sending the surrender to the other player
                    EndGame(currentGame.CurrentTime, currentGame.endType, currentGame.winner);
                break;
            default:
                break;
        }

        if(!multiplayer)
            EndGame(currentGame.CurrentTime, currentGame.endType, currentGame.winner);
    }

    public void HexChessGameEnded()
    {
        // Game.onGameOver may be invoked off of the main thread (in the case of a flagfall only).
        // Because of this, we need to communicate back to the main thread that the game has ended so that we may prcoess our game over.
        // Because the Update() method is always executed on the main thread, we can flip a bool and check for that flip in our update loop.
        // We must use a lock here so that flipping the bool is sync'd back to the main thread.
        lock(lockObj)
        {
            hexGameOver = true;
        }
    } 

    public BoardState MovePiece(IPiece piece, Index targetLocation, BoardState boardState)
    {
        // No promotions are happening in here, so we can just assume queen since it won't change anything
        
        // I was for some reason using realPiece in QueryMove. 
        // That's obviously wrong, but I remember it fixing a bug, I just forgot which one.
        // If we use realPiece, the wrong piece is moved when stepping through history/loading games in the case of a promotion
        // Piece realPiece = currentGame.GetRealPiece((piece.team, piece.piece)); 
        var newStateWithPromos = currentGame.QueryMove((piece.team, piece.piece), (targetLocation, MoveType.Move), boardState, Piece.Queen);
        
        // If there is a piece at the target location, capture it if it is an enemy
        if(boardState.TryGetPiece(targetLocation, out (Team occupyingTeam, Piece occupyingType) teamedPiece))
        {
            if(teamedPiece.occupyingTeam != piece.team)
            {
                // Debug.Log($"{piece.team} : {piece.piece} at {piece.location.GetKey()} to capture {teamedPiece.occupyingTeam} : {teamedPiece.occupyingType} at {targetLocation.GetKey()}");
                IPiece occupyingPiece = activePieces[teamedPiece];

                // Capture the enemy IPiece
                if(occupyingPiece.piece == Piece.King)
                    Debug.LogError("Kings can't be captured. Game should not allow this state to occur.");
                jails[(int)teamedPiece.occupyingTeam].Enprison(occupyingPiece);
                activePieces.Remove(teamedPiece);
            }
        }

        // Trigger move for IPiece
        piece.MoveTo(GetHexIfInBounds(targetLocation));

        // Update move tracker
        List<BoardState> hist = new List<BoardState>(currentGame.turnHistory);
        hist.Add(newStateWithPromos.newState);
        Move move = HexachessagonEngine.GetLastMove(hist, currentGame.promotions, isFreeplaced);
        moveTracker?.UpdateText(move);
        
        return newStateWithPromos.newState;
    }

    public void MovePieceForPromotion(IPiece piece, Hex targetLocation, BoardState boardState)
    {
        // If the hex being moved into contains an enemy piece, capture it
        if(boardState.TryGetPiece(targetLocation.index, out (Team team, Piece type) occupyingPiece))
        {
            if(occupyingPiece.type == Piece.King)
                Debug.LogError("Kings can't be captured!");
            else if(occupyingPiece.team != piece.team)
            {
                IPiece occupyingIPiece = activePieces[occupyingPiece];
                // Capture the enemy IPiece
                jails[(int)occupyingPiece.team].Enprison(occupyingIPiece);
                activePieces.Remove(occupyingPiece);
            }
        }

        Index startLoc = piece.location;

        piece.MoveTo(targetLocation, (Piece promoteTo) => {
            Piece realPiece = currentGame.GetRealPiece((piece.team, piece.piece));
            var newStateWithPromos = currentGame.QueryMove((piece.team, realPiece), (targetLocation.index, MoveType.Move), boardState, promoteTo, currentGame.GetTurnCount());
            currentGame.SetPromotions(newStateWithPromos.promotions);
            AdvanceTurn(newStateWithPromos.newState);
            moveTracker.UpdateText(currentGame.GetLastMove(isFreeplaced));
        });
    }

    public void QueryPromote(Pawn pawn, Action<Piece> action)
    {
        // We don't want to display the query promote screen if we're not the team making the promote
        // That information will arrive to us across the network
        Multiplayer multiplayer = GameObject.FindObjectOfType<Multiplayer>();
        if(multiplayer != null && multiplayer.localTeam != GetCurrentTurn())
            return;
            
        promotionDialogue.Display(pieceType => {
            PromoteIPiece(pawn, pieceType);
            int promoTurnCount = currentGame.GetTurnCount() + 1; // +1 because we haven't yet applied the move to the game
            action?.Invoke(pieceType);

            multiplayer?.SendPromote(new Promotion(pawn.team, pawn.piece, pieceType, promoTurnCount));
        });
    }

    public IPiece PromoteIPiece(Pawn pawn, Piece type)
    {
        // Replace the pawn with the chosen piece type
        // Worth noting: Even though the new IPiece is of a different type than Pawn,
        // we still use the PieceType.Pawn# (read from the pawn) to store it's position in the game state to maintain it's unique key
        // This means that anytime you might care about promotions, you need to cross reference the list of promotions to see if any are applicable
        Hex hex = GetHexIfInBounds(pawn.location);

        IPiece newPiece = Instantiate(piecePrefabs[(pawn.team, type)], hex.transform.position + Vector3.up, Quaternion.identity).GetComponent<IPiece>();
        newPiece.Init(pawn.team, pawn.piece, pawn.location);
        activePieces[(pawn.team, pawn.piece)] = newPiece;
        Destroy(pawn.gameObject);
        return newPiece;
    }

    private IPiece GetPromotedPieceIfNeeded(IPiece piece, int? turn = null)
    {
        if(piece is Pawn pawn)
        {
            Piece p = pawn.piece;

            if(currentGame.TryGetApplicablePromo((piece.team, piece.piece), turn.HasValue ? turn.Value : int.MaxValue, out Promotion promo))
                p = promo.to;

            if(p != pawn.piece)
                piece = PromoteIPiece(pawn, p);
        }

        return piece;
    }

    public BoardState Swap(IPiece p1, IPiece p2, BoardState boardState)
    {
        if(p1 == p2)
            return boardState;

        // BoardState currentState = boardState;
        boardState.TryGetIndex((p1.team, p1.piece), out Index p1StartLoc);
        boardState.TryGetIndex((p2.team, p2.piece), out Index p2StartLoc);
        
        var newStateWithPromos = currentGame.QueryMove(p1StartLoc, (p2StartLoc, MoveType.Defend), boardState, Piece.Queen);

        // Update move tracker
        moveTracker?.UpdateText(new Move(
            currentGame.GetTurnCount() + 1, // +1 because we have yet to apply the move to the game
            p1.team,
            p1.piece,
            p1StartLoc,
            p2StartLoc,
            null,
            p2.piece
        ));

        // Move the 2 IPieces
        p1.MoveTo(GetHexIfInBounds(p2StartLoc));
        p2.MoveTo(GetHexIfInBounds(p1StartLoc));

        return newStateWithPromos.newState;
    }

    public BoardState EnPassant(Pawn pawn, Team enemyTeam, Piece enemyPiece, Index targetLocation, BoardState boardState)
    {
        // An enpassant can never end with a pawn on a promotion hex, so we can just use Queen for our PromoteTo param as it doesn't matter here
        var newStateWithPromos = currentGame.QueryMove((pawn.team, pawn.piece), (targetLocation, MoveType.EnPassant), boardState, Piece.Queen);

        // Capture enemy IPiece
        IPiece enemyIPiece = activePieces[(enemyTeam, enemyPiece)];
        if(enemyPiece == Piece.King)
            Debug.LogError("Kings can't be captured.");
        activePieces.Remove((enemyTeam, enemyPiece));
        jails[(int)enemyTeam].Enprison(enemyIPiece);

        // Update move tracker
        moveTracker?.UpdateText(new Move(
            currentGame.GetTurnCount() + 1,
            pawn.team,
            pawn.piece,
            pawn.location,
            targetLocation,
            enemyPiece,
            null
        ));

        // Move pawn
        pawn.MoveTo(GetHexIfInBounds(targetLocation));

        return newStateWithPromos.newState;
    }

    public void Enprison(IPiece toPrison, bool updateState = true)
    {
        jails[(int)toPrison.team].Enprison(toPrison);
        activePieces.Remove((toPrison.team, toPrison.piece));

        if(updateState)
        {
            BoardState newState = currentGame.Enprison((toPrison.team, toPrison.piece));
            AdvanceTurn(newState);
        }
    }

    public void EndGame(float timestamp, GameEndType endType = GameEndType.Pending, Winner winner = Winner.Pending)
    {
        BoardState currentState = GetCurrentBoardState();

        Multiplayer multiplayer = GameObject.FindObjectOfType<Multiplayer>();
        if(multiplayer)
        {
            Team winningTeam = Team.None;
            if(winner == Winner.White)
                winningTeam = Team.White;
            else if(winner == Winner.Black)
                winningTeam = Team.Black;

            if(!surpressVictoryAudio)
            {
                if(winner == Winner.Draw)
                    audioSource.PlayOneShot(drawAudio);
                else if(multiplayer.gameParams.localTeam == winningTeam)
                    audioSource.PlayOneShot(winFanfare);
                else
                    audioSource.PlayOneShot(loseKnell);
            }
        }
        else if(!surpressVictoryAudio)
        {
            if(winner == Winner.Draw)
                audioSource.PlayOneShot(drawAudio);
            else
                audioSource.PlayOneShot(winFanfare);
        }

        gameOver?.Invoke(currentGame);
    }

    public void HighlightMove(Move move)
    {
        ClearMoveHighlight();

        if(TryGetHexIfInBounds(move.from, out Hex fromHex))
        {
            fromHex.Highlight(lastMoveHighlightColor);
            highlightedHexes.Add(fromHex);
        }

        if(TryGetHexIfInBounds(move.to, out Hex toHex))
        {
            toHex.Highlight(move.capturedPiece.HasValue
                ? Color.red
                : move.defendedPiece.HasValue
                    ? Color.green
                    : lastMoveHighlightColor
            );
            highlightedHexes.Add(toHex);
        }
    }

    private void ClearMoveHighlight()
    {
        foreach(Hex hex in highlightedHexes)
            hex.Unhighlight();
        highlightedHexes.Clear();
    }

    public void ResetPieces(Game game = null) 
    {
        Game toLoad = game == null ? GetGame(gamesToLoadLoc[UnityEngine.Random.Range(0, gamesToLoadLoc.Count)]) : game;
        LoadGame(toLoad);
    }

    public void Reset()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        SceneTransition sceneTransition = GameObject.FindObjectOfType<SceneTransition>();
        if(sceneTransition != null)
            sceneTransition.Transition(sceneName);
        else
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    [Button("Spawn Hexes")]
    private void SpawnHexes()
    {
        if(hexes.Count > 0)
            ClearHexes();
        
        for(int row = 0; row < HexGrid.rows; row++)
        {
            hexes.Add(new List<Hex>());
            for(int col = 0; col < HexGrid.cols; col++)
            {
                if(HexGrid.cols % 2 != 0 && col == HexGrid.cols - 1 && row % 2 == 0)
                    continue;

                GameObject newGo = Instantiate(
                    original: hexPrefab,
                    position: new Vector3(
                        x: hexGrid.radius * 3 * col + Get_X_Offset(row),
                        y: UnityEngine.Random.Range(hexGrid.minHeight, hexGrid.maxHeight),
                        z: row * hexGrid.Apothem
                    ),
                    rotation: Quaternion.identity,
                    parent: transform
                );

                Hex newHex = newGo.GetComponent<Hex>();

                newHex.transform.localScale = new Vector3(
                    x: newHex.transform.localScale.x * hexGrid.radius,
                    y: newHex.transform.localScale.y * hexGrid.height,
                    z: newHex.transform.localScale.z * hexGrid.radius
                );

                newHex.AssignIndex(new Index(row, col), this);

                hexes[row].Add(newHex);
                newHex.SetColor(GetColor(row));
            }
        }
    }

    public Color GetColor(int row) => row % 2 == 0  
        ? hexGrid.colors[(Mathf.FloorToInt(row/2) + 1) % 3]
        : hexGrid.colors[Mathf.FloorToInt(row/2) % 3];

    private float Get_X_Offset(int row) => row % 2 == 0 ? hexGrid.radius * 1.5f : 0f;

    [Button("Clear Hexes")]
    private void ClearHexes()
    {
        for(int row = 0; row < hexes.Count; row++)
        {
            for(int col = 0; col < hexes[row].Count; col++)
            {
#if UNITY_EDITOR
                DestroyImmediate(hexes[row][col].gameObject);
#elif !UNITY_EDITOR
                Destroy(hexes[row][col].gameObject);
#endif                
            }
        }
        hexes = new List<List<Hex>>();
    }

    public Hex GetNeighborAt(Index source, HexNeighborDirection direction)
    {
        Index? neighbor = HexGrid.GetNeighborAt(source, direction);
        if (neighbor.HasValue)
            return GetHexIfInBounds(neighbor.Value);
        return null;
    }
    public Hex GetHexIfInBounds(int row, int col) => 
        HexGrid.IsInBounds(row, col) ? hexes[row][col] : null;
    public Hex GetHexIfInBounds(Index index) => 
        GetHexIfInBounds(index.row, index.col);

    public bool TryGetHexIfInBounds(int row, int col, out Hex hex)
    {
        hex = GetHexIfInBounds(row, col);
        return hex != null;
    }
    public bool TryGetHexIfInBounds(Index index, out Hex hex)
    {
        hex = GetHexIfInBounds(index);
        return hex != null;
    }
    
    public IEnumerable<Hex> GetHexesInCol(int col)
    {
        List<Hex> hexesInCol = new List<Hex>();
        for(int i = 0; i < hexes.Count; i++)
        {
            for(int j = 0; j < hexes[i].Count; j++)
            {
                if(j == col)
                {
                    Hex hex = GetHexIfInBounds(i, j);
                    if(hex != null)
                        hexesInCol.Add(hex);
                }
            }
        }
        return hexesInCol;
    }
}