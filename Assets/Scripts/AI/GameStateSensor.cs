using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class GameStateSensor : ISensor
{
    string name;
    ObservationSpec observationSpec;
    Board board;
    public GameStateSensor(Board board)
    {
        this.board = board;
        this.name = "GameStateSensor";

        // This might be 85 or 85*3
        observationSpec = ObservationSpec.Vector(2);
    }
    public byte[] GetCompressedObservation() => null;
    public CompressionSpec GetCompressionSpec() => new CompressionSpec(SensorCompressionType.None);
    public string GetName() => name;

    public ObservationSpec GetObservationSpec() => observationSpec;

    public void Reset(){}

    public void Update(){}

    public int Write(ObservationWriter writer)
    {
        BoardState state = board.GetCurrentBoardState();
        writer[0] = (float)state.currentMove;
        // progression towards 50 move rule
        writer[1] = board.currentGame.turnsSincePawnMovedOrPieceTaken;

        return 2;
    }
}
