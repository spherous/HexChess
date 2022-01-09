using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class BoardSensor : ISensor
{
    string name;
    ObservationSpec observationSpec;
    Board board;
    public BoardSensor(Board board)
    {
        this.board = board;
        this.name = "BoardSensor";

        // This might be 85 or 85*3
        observationSpec = ObservationSpec.Vector(85*3);
    }

    // public static BoardSensor Create(Board board) => new BoardSensor(board);

    public byte[] GetCompressedObservation() => null;

    public CompressionSpec GetCompressionSpec() => new CompressionSpec(SensorCompressionType.None);

    public string GetName() => name;

    public ObservationSpec GetObservationSpec() => observationSpec;

    public void Reset(){}

    public void Update(){}

    public int Write(ObservationWriter writer)
    {
        BoardState state = board.GetCurrentBoardState();
        int offset = 0;

        foreach(Index index in Index.GetAllIndices())
        {
            if(state.TryGetPiece(index, out (Team team, Piece piece) teamedPiece))
            {
                Piece realPiece = board.currentGame.GetRealPiece(teamedPiece);

                writer.Add(new Vector3(
                    (float)teamedPiece.team,
                    ((float)realPiece) + 1f,
                    index.GetSingleVal()
                ), offset);
            }
            else
                writer.Add(new Vector3(0, 0, index.GetSingleVal()), offset);

            offset += 3;
        }
        
        return offset;
    }
}