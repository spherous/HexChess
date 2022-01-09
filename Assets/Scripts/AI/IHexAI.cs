#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IHexAI
{
    public Task<HexAIMove> GetMove(Game game);
    public void CancelMove();
    public IEnumerable<string>? GetDiagnosticInfo();
}
