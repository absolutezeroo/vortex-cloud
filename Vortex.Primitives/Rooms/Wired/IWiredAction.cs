using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredAction : IWiredBox
{
    public int GetDelayMs();
    public Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct);
}
