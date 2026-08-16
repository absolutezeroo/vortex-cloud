using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredAction : IWiredBox
{
    public int GetDelayMs();

    /// <summary>
    /// Whether this action belongs to the pile's negative branch — the one that runs when the
    /// trigger fired but the conditions did not hold. The furni say so themselves ("WIRED Negative
    /// Effect"), and without the distinction they would run on success, which is the opposite of
    /// what they mean.
    /// </summary>
    public bool IsNegative();
    public Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct);
}
