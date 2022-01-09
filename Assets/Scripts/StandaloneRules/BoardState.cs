using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;


public struct BoardState
{
    public static readonly BoardState defaultState = GetBoardStateFromDeserializedBoard(SerializeableGame.defaultSerializedBoard.pieces, SerializeableGame.defaultSerializedBoard.team, SerializeableGame.defaultSerializedBoard.check, SerializeableGame.defaultSerializedBoard.checkmate, SerializeableGame.defaultSerializedBoard.duration);
    public Team currentMove;
    public BidirectionalDictionary<(Team team, Piece piece), Index> allPiecePositions;
    public Team check;
    public Team checkmate;
    public float executedAtTime;

    public BoardState(BidirectionalDictionary<(Team, Piece), Index> allPiecePositions, Team currentMove, Team check, Team checkmate, float executedAtTime)
    {
        this.allPiecePositions = allPiecePositions;
        this.currentMove = currentMove;
        this.check = check;
        this.checkmate = checkmate;
        this.executedAtTime = executedAtTime;
    }

    public BoardState Enprison((Team team, Piece piece) teamedPiece) =>
        HexachessagonEngine.Enprison(this, teamedPiece);

    public readonly bool TryGetPiece(Index index, out (Team team, Piece piece) teamedPiece) => 
        allPiecePositions.TryGetValue(index, out teamedPiece);
    public readonly bool TryGetIndex((Team team, Piece piece) teamedPiece, out Index index) => 
        allPiecePositions.TryGetValue(teamedPiece, out index);

    public static bool operator ==(BoardState a, BoardState b) => 
        a.currentMove == b.currentMove 
        && a.allPiecePositions.Count == b.allPiecePositions.Count 
        && !a.allPiecePositions.Except(b.allPiecePositions).Any();
    public static bool operator !=(BoardState a, BoardState b) => !(a == b);
    public override bool Equals(object obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();

    public readonly bool IsOccupied(Index index) => allPiecePositions.ContainsKey(index);
    public readonly bool IsOccupiedBy(Index index, (Team expectedTeam, Piece expectedPiece) expected)
    {
        if(!TryGetPiece(index, out (Team actualTeam, Piece actualPiece) actual))
            return false;

        return expected == actual;
    }

    // Json (including newtonsoft) can not properly serialize a dictionary that uses a key that is any type other than than a string.
    // To get around this, we convert our Bidirectional dictionary into a list, then serialze that list.
    public List<SerializedPiece> GetSerializeable()
    {
        List<SerializedPiece> list = new List<SerializedPiece>();
        foreach(KeyValuePair<(Team, Piece), Index> kvp in allPiecePositions)
        {
            (Team team, Piece piece) = kvp.Key;
            list.Add(new SerializedPiece{t = team, p = piece, i = kvp.Value});
        }
        return list;
    }

    public byte[] Serialize() =>
        Encoding.ASCII.GetBytes(
            JsonConvert.SerializeObject(new SerializedBoard{
                pieces = GetSerializeable(),
                currentMove = currentMove,
                check = check,
                checkmate = checkmate,
                executedAtTime = executedAtTime
            })
        );

    // When deserializing from json, because of the before mentioned dictionary issues, we must deserialize as a list, then construct our dictionary from it.
    public static BoardState GetBoardStateFromDeserializedBoard(List<SerializedPiece> list, Team currentMove, Team check, Team checkmate, float executedAtTime)
    {
        BidirectionalDictionary<(Team, Piece), Index> newDict = new BidirectionalDictionary<(Team, Piece), Index>();
        foreach(SerializedPiece tpl in list)
            newDict.Add((tpl.t, tpl.p), tpl.i);

        return new BoardState(newDict, currentMove, check, checkmate, executedAtTime);
    }

    public static BoardState Deserialize(byte[] data)
    {
        string json = Encoding.ASCII.GetString(data);
        SerializedBoard boardstate = JsonConvert.DeserializeObject<SerializedBoard>(json);
        
        BidirectionalDictionary<(Team, Piece), Index> newDict = new BidirectionalDictionary<(Team, Piece), Index>();
        foreach(SerializedPiece tpl in boardstate.pieces)
            newDict.Add((tpl.t, tpl.p), tpl.i);

        return new BoardState(newDict, boardstate.currentMove, boardstate.check, boardstate.checkmate, boardstate.executedAtTime);
    }
}

[System.Serializable]
public struct SerializedBoard {
    public List<SerializedPiece> pieces;
    public Team currentMove;
    public Team check;
    public Team checkmate;
    public float executedAtTime;
}

[System.Serializable]
public struct SerializedPiece {
    public Team t;
    public Piece p;
    public Index i;
}