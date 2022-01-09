using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

[System.Serializable]
public struct SerializeableGame
{
    public static readonly SerializeableGame defaultGame = SerializeableGame.Deserialize(DefaultBoard.json);
    public static readonly (Team team, List<SerializedPiece> pieces, Team check, Team checkmate, float duration) defaultSerializedBoard = defaultGame.serializedBoards.FirstOrDefault();

    public List<(Team, List<SerializedPiece>, Team, Team, float)> serializedBoards;
    public List<Promotion> promotions;
    public Winner winner;
    public GameEndType endType;
    public float timerDuration;
    public bool hasClock;

    public SerializeableGame(Game game)
    {
        serializedBoards = new List<(Team, List<SerializedPiece>, Team, Team, float)>();
        foreach(BoardState bs in game.turnHistory)
        {
            List<SerializedPiece> serializeableBoardState = bs.GetSerializeable();
            serializedBoards.Add((bs.currentMove, serializeableBoardState, bs.check, bs.checkmate, bs.executedAtTime));
        }

        this.promotions = game.promotions;
        this.winner = game.winner;
        this.endType = game.endType;
        this.timerDuration = game.timerDuration;
        this.hasClock = game.hasClock;
    }

    public string Serialize() => JsonConvert.SerializeObject(this);
    public static SerializeableGame Deserialize(string json) => JsonConvert.DeserializeObject<SerializeableGame>(json);

    public List<BoardState> GetHistory()
    {
        List<BoardState> history = new List<BoardState>();
        foreach((Team team, List<SerializedPiece> pieces, Team check, Team checkmate, float duration) in serializedBoards)
            history.Add(BoardState.GetBoardStateFromDeserializedBoard(pieces, team, check, checkmate, duration));
        return history;
    }
}