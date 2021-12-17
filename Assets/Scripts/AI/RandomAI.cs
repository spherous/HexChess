using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class RandomAI : IHexAI
{
    Random random = new Random();
    public Task<HexAIMove> GetMove(Game game)
    {
        var allmoves = HexAIMove.GenerateAllValidMoves(game).ToArray();
        return Task.FromResult(allmoves[random.Next(0, allmoves.Length)]);
    }

    public void CancelMove() { }

    public IEnumerable<string> GetDiagnosticInfo()
    {
        return null;
    }
}
