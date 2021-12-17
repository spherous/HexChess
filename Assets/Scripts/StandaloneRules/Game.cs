using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Extensions;

public class Game 
{
    public Winner winner {get; private set;}
    public GameEndType endType {get; private set;}
    public List<BoardState> turnHistory {get; private set;}
    public List<Promotion> promotions {get; private set;}
    public float timerDuration;
    public bool hasClock;
    public int turnsSincePawnMovedOrPieceTaken;
    float timeOffset;
    public float CurrentTime => whiteTimekeeper.elapsed + blackTimekeeper.elapsed + timeOffset;

    public Timekeeper whiteTimekeeper {get; private set;}
    public Timekeeper blackTimekeeper {get; private set;}

    public delegate void GameOver();
    public GameOver onGameOver;

    public Game(
        List<BoardState> history, 
        List<Promotion> promotions = null, 
        Winner winner = Winner.Pending, 
        GameEndType endType = GameEndType.Pending, 
        float timerDuration = 0, 
        bool hasClock = false
    )
    {
        turnHistory = history;
        this.winner = winner;
        this.promotions = promotions == null ? new List<Promotion>() : promotions;
        this.endType = endType;
        this.timerDuration = timerDuration;
        this.hasClock = hasClock;
        turnsSincePawnMovedOrPieceTaken = 0;

        timeOffset = GetCurrentBoardState().executedAtTime;

        // When loading a game that has already ended, we do not need to start the time keepers
        StartTimekeeper();
        if(endType != GameEndType.Pending)
        {
            RecalculateTimekeepers();
            whiteTimekeeper.Pause();
            blackTimekeeper.Pause();
        }
    }

    public Game(SerializeableGame fromGame) : this(fromGame.GetHistory(), fromGame.promotions, fromGame.winner, fromGame.endType, fromGame.timerDuration, fromGame.hasClock){}
    ~Game() => KillGame();

    public string Serialize() => new SerializeableGame(this).Serialize();
    public static Game Deserialize(string json) => new Game(SerializeableGame.Deserialize(json));

    public static Game CreateNewGame() => new Game(SerializeableGame.defaultGame);

    public void ChangeTimeParams(bool showClock, float timerDuration)
    {
        hasClock = showClock;
        this.timerDuration = timerDuration;

        whiteTimekeeper.duration = !hasClock ? timerDuration > 0 ? timerDuration : (float?)null : (float?)null;
        blackTimekeeper.duration = !hasClock ? timerDuration > 0 ? timerDuration : (float?)null : (float?)null;
    }

    public void StartTimekeeper()
    {
        whiteTimekeeper = new Timekeeper(!hasClock ? timerDuration > 0 ? timerDuration : (float?)null : (float?)null);
        blackTimekeeper = new Timekeeper(!hasClock ? timerDuration > 0 ? timerDuration : (float?)null : (float?)null);

        whiteTimekeeper.onTimerElapsed += () => Flagfall(Team.White);
        blackTimekeeper.onTimerElapsed += () => Flagfall(Team.Black);

        RecalculateTimekeepers();

        Team currentTurn = GetCurrentTurn();
        Timekeeper toPlay = GetCurrentTurn() == Team.White ? whiteTimekeeper : blackTimekeeper;
        
        toPlay.Play();
    }
    public void RecalculateTimekeepers()
    {
        float whiteTotal = 0f;
        float blackTotal = 0f; 

        for(int i = 1; i < turnHistory.Count; i++)
        {
            BoardState nowState = turnHistory[i];
            BoardState lastState = turnHistory[i - 1];
            float duration = nowState.executedAtTime - lastState.executedAtTime;

            if(nowState.currentMove == Team.None)
            {
                if(lastState.currentMove == Team.White)
                    whiteTotal += duration;
                else if(lastState.currentMove == Team.Black)
                    blackTotal += duration;
            }
            else if(i % 2 == 0)
                blackTotal += duration;
            else
                whiteTotal += duration;
        }

        whiteTimekeeper.SetTime(whiteTotal);
        blackTimekeeper.SetTime(blackTotal);
    }

    public void AddState(BoardState newState) => turnHistory.Add(newState);
    public void AddPromotion(Promotion promo) => promotions.Add(promo);
    public void SetPromotions(List<Promotion> promotions) => this.promotions = promotions;

    public void Surrender(Team surrenderingTeam, float? timestamp = null) => 
        EndGame(GameEndType.Surrender, surrenderingTeam == Team.White ? Winner.Black : Winner.White, timestamp);
    public void Flagfall(Team teamOutOfTime) => 
        EndGame(GameEndType.Flagfall, teamOutOfTime == Team.White ? Winner.Black : Winner.White);
    public void EndGame(GameEndType endType, Winner winner, float? timestamp = null)
    {
        whiteTimekeeper?.Pause();
        blackTimekeeper?.Pause();

        BoardState currentState = GetCurrentBoardState();
        this.endType = endType;
        this.winner = winner;
        currentState.currentMove = Team.None;
        currentState.executedAtTime = timestamp.HasValue ? timestamp.Value : CurrentTime;
        AddState(currentState);

        onGameOver?.Invoke();
        KillGame();
    }

    public void KillGame()
    {
        // If the game ends because the timer ran out of time this will be called on that timer's thread. 
        // If that's the case, we want to terminate the other timekeeper's thread before we kill the thread we're currently on
        // If it was called from a non-timer thread, we can simply kill both timekeepers.
        if(whiteTimekeeper != null && Thread.CurrentThread == whiteTimekeeper.trackedThread.Thread)
        {
            blackTimekeeper?.Stop();
            whiteTimekeeper.Stop();
        }
        else if(blackTimekeeper != null && Thread.CurrentThread == blackTimekeeper.trackedThread.Thread)
        {
            whiteTimekeeper?.Stop();
            blackTimekeeper.Stop();
        }
        else if(whiteTimekeeper != null && blackTimekeeper != null)
        {
            whiteTimekeeper.Stop();
            blackTimekeeper.Stop();
        }
    }

    public BoardState Enprison((Team team, Piece piece) teamedPiece) => 
        HexachessagonEngine.Enprison(GetCurrentBoardState(), teamedPiece);

    public void AdvanceTurn(BoardState newState, bool isFreeplaced = false, bool updateTime = true)
    {
        float timestamp = CurrentTime;
        Team otherTeam = newState.currentMove.Enemy();
        
        if(updateTime)
            newState.executedAtTime = timestamp;
        
        if(newState.currentMove != Team.None)
        {
            Timekeeper toPlay = newState.currentMove == Team.White ? whiteTimekeeper : blackTimekeeper;
            Timekeeper toPause = newState.currentMove == Team.White ? blackTimekeeper : whiteTimekeeper;
            toPlay.Play();
            toPause.Pause();
        }
        else
        {
            whiteTimekeeper.Pause();
            blackTimekeeper.Pause();
        }

        // When a checkmate occurs, the game is over. The team that was checkmate'd loses, while the other team wins        
        newState = ResetCheck(newState);
        newState = CheckForCheckAndMate(newState);

        if(newState.checkmate != Team.None)
        {
            AddState(newState);
            EndGame(GameEndType.Checkmate, newState.checkmate == Team.White ? Winner.Black : Winner.White);
            return;
        }

        // When the team who's turn it is becoming has 0 valid moves, a stalemate has occured
        var otherTeamValidMoves = GetAllValidMovesForTeam(newState.currentMove, newState);
        bool noValidMovesForNextPlayer = !otherTeamValidMoves.Any();

        // Check for insufficient material, stalemate if both teams have insufficient material
        IEnumerable<Piece> whitePieces = GetRemainingPiecesForTeam(Team.White, newState);
        IEnumerable<Piece> blackPieces = GetRemainingPiecesForTeam(Team.Black, newState);
        bool whiteSufficientMaterial = true;
        bool blackSufficientMaterial = true;
        foreach(List<Piece> insufficientSet in HexachessagonEngine.insufficientSets)
        {
            whiteSufficientMaterial = whiteSufficientMaterial ? whitePieces.Except(insufficientSet).Any() : false;
            blackSufficientMaterial = blackSufficientMaterial ? blackPieces.Except(insufficientSet).Any() : false;
        }

        if(!isFreeplaced) // When using freeplace mode, the game shouldn't end on stalemates (things might stale while you try to setup a challenge board)
        {
            if(noValidMovesForNextPlayer || (!whiteSufficientMaterial && !blackSufficientMaterial))
            {
                AddState(newState);
                EndGame(GameEndType.Stalemate, Winner.None);
                return;
            }
        }

        // Check for 5 fold repetition
        // When the same board state occurs 5 times in a game, the game ends in a draw
        if(GetFiveFoldProgress(newState) >= 5)
        {
            AddState(newState);
            EndGame(GameEndType.Draw, Winner.Draw);
            return;
        }

        AddState(newState);

        // The game ends in a draw due to 50 move rule (50 turns of both teams playing with no captured piece, or moved pawn)
        Move newMove = GetLastMove();
        Piece lastRealPiece = GetRealPiece((newMove.lastTeam, newMove.lastPiece));

        turnsSincePawnMovedOrPieceTaken = newMove.capturedPiece.HasValue || lastRealPiece >= Piece.Pawn1 
            ? 0 
            : turnsSincePawnMovedOrPieceTaken + 1;

        if(turnsSincePawnMovedOrPieceTaken == 100)
        {
            EndGame(GameEndType.Draw, Winner.Draw);
            return;
        }

        RecalculateTimekeepers();
    }

    public bool TryGetApplicablePromo((Team team, Piece piece) teamedPiece, int turnNumber, out Promotion promotion) =>
        HexachessagonEngine.TryGetApplicablePromo(teamedPiece, turnNumber, out promotion, promotions);

    public Piece GetRealPiece((Team team, Piece piece) teamedPiece) =>
        HexachessagonEngine.GetRealPiece(teamedPiece, promotions);
    
    public Piece GetRealPiece((Team team, Piece piece) teamedPiece, int turnNumber) =>
        HexachessagonEngine.GetRealPiece(teamedPiece, promotions, turnNumber);

    public bool CheckFiveFoldProgress(BoardState toCheck) => turnHistory.Any(state => state == toCheck);
    public int GetFiveFoldProgress(BoardState toCheck) => turnHistory.Count(state => state == toCheck);
    
    public int GetFiftyTurnRuleCount()
    {
        int count = 0;
        for(int i = 0; i < turnHistory.Count - 1; i++)
        {
            Move moveStep = HexachessagonEngine.GetLastMove(turnHistory.Skip(i).Take(2).ToList(), promotions);
            count = moveStep.capturedPiece.HasValue || moveStep.lastPiece >= Piece.Pawn1 
                ? 0 
                : count + 1;
        }
        return count;
    }

    public int GetTurnCount() => 
        (endType == GameEndType.Draw || endType == GameEndType.Checkmate || endType == GameEndType.Stalemate) && turnHistory[turnHistory.Count - 2].currentMove == Team.White
            ? ((float)turnHistory.Count / 2f).FloorToInt() - 1 
            : ((float)turnHistory.Count / 2f).FloorToInt();
    public BoardState GetCurrentBoardState() => turnHistory[turnHistory.Count - 1];
    public float GetGameLength() => GetCurrentBoardState().executedAtTime;
    public Team GetCurrentTurn() => GetCurrentBoardState().currentMove;
    public Team GetLastTurn() => turnHistory.Count > 1 ? turnHistory[turnHistory.Count - 2].currentMove : Team.None;

    public BoardState CheckForCheckAndMate(BoardState state) =>
        MoveValidator.CheckForCheckAndMate(state, promotions);
    public bool IsChecking(Team checkForTeam, BoardState state) =>
        MoveValidator.IsChecking(checkForTeam, state, promotions);
    public BoardState ResetCheck(BoardState newState)
    {
        if(turnHistory.Count > 0)
        {
            BoardState oldState = GetCurrentBoardState();
            if(oldState.check != Team.None)
            {
                newState.check = Team.None;
                newState.checkmate = Team.None;
            }
        }
        else
        {
            newState.check = Team.None;
            newState.checkmate = Team.None;
        }
        return newState;
    }

    public Move GetLastMove(bool isFreeplaced = false) =>
        HexachessagonEngine.GetLastMove(turnHistory, promotions, isFreeplaced);

    // Anytime we are not taking a piece out with freeplace mode, we can extrapolate teamedPiece from start index, thus that is all that is required
    public (BoardState newState, List<Promotion> promotions) QueryMove(Index start, (Index target, MoveType moveType) move, BoardState state, Piece promoteTo, int? turnCount = null) =>
        HexachessagonEngine.QueryMove(start, move, state, promoteTo, promotions, turnCount.HasValue ? turnCount.Value : GetTurnCount());
    
    // When using freeplace mode to take a piece out of the jail, we have no start location, thus we must take in the teamedPiece
    public (BoardState newState, List<Promotion> promotions) QueryMove((Team team, Piece piece) teamedPiece, (Index target, MoveType moveType) move, BoardState state, Piece promoteTo, int? turnCount = null) =>
        HexachessagonEngine.QueryMove(teamedPiece, move, state, promoteTo, promotions, turnCount.HasValue ? turnCount.Value : GetTurnCount());

    public IEnumerable<Piece> GetRemainingPiecesForTeam(Team team, BoardState state) => 
        HexachessagonEngine.GetRemainingPiecesForTeam(team, state, promotions);
    
    public List<(Piece piece, Index index, MoveType moveType)> GetAllValidMovesForTeam(Team team, BoardState state) =>
        MoveGenerator.GetAllValidMovesForTeam(team, state, promotions);

    public IEnumerable<Index> GetValidAttacksConcerningHex(Index hexIndex, BoardState state) =>
        MoveGenerator.GetValidAttacksConcerningHex(hexIndex, state, promotions);

    public IEnumerable<(Index target, MoveType moveType)> GetAllValidMovesForPiece((Team team, Piece piece) teamedPiece, BoardState boardState, bool includeBlocking = false) =>
        MoveGenerator.GetAllValidMovesForPiece(teamedPiece, boardState, promotions, includeBlocking);

    public IEnumerable<Index> GetAllValidAttacksForPieceConcerningHex((Team team, Piece piece) teamedPiece, BoardState boardState, Index hexIndex, bool includeBlocking = false) => 
        MoveGenerator.GetAllValidAttacksForPieceConcerningHex(teamedPiece, boardState, hexIndex, promotions, includeBlocking);
    
    public IEnumerable<Index> GetAllValidTheoreticalAttacksFromTeamConcerningHex(Team team, Index hexIndex, BoardState state) =>
        MoveGenerator.GetAllValidTheoreticalAttacksFromTeamConcerningHex(team, hexIndex, state, promotions);
    
    public IEnumerable<Index> GetAllTheoreticalAttacksForPieceConcerningHex((Team team, Piece piece) teamedPiece, BoardState boardState, Index hexIndex, bool includeBlocking = false) =>
        MoveGenerator.GetAllTheoreticalAttacksForPieceConcerningHex(teamedPiece, boardState, hexIndex, promotions, includeBlocking);
}