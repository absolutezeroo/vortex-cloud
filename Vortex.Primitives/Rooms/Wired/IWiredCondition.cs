using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredCondition : IWiredBox
{
    public int GetQuantifierCode();
    public bool GetIsInvert();
    public byte GetQuantifierType();
    public bool IsNegative();

    /// <summary>
    /// The condition's chance to fetch what it cannot fetch from <see cref="Evaluate"/>, run by the
    /// wired engine immediately before the stack's conditions are evaluated.
    /// </summary>
    /// <remarks>
    /// <see cref="Evaluate"/> is synchronous on purpose — it runs inside the room's own turn and a
    /// predicate that awaited would let the room state move underneath the stack it is deciding on.
    /// A condition that needs something from outside the room (a guild roster, a player's badges)
    /// warms a room-side cache here and then reads it synchronously. A failure to prepare is not
    /// fatal: the evaluation simply finds nothing cached and does not pass.
    /// </remarks>
    public Task PrepareAsync(IWiredProcessingContext ctx, CancellationToken ct);

    public bool Evaluate(IWiredProcessingContext ctx);
}
