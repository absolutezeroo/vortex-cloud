using System.Collections.Generic;
using System.Collections.Immutable;
using Vortex.Primitives.Signals;

namespace Vortex.Signals;

/// <summary>
/// Asks every consumer whether anyone would do anything with an action.
/// </summary>
/// <remarks>
/// <para>
/// The union is never cached. Each source exposes a live set — reward tracks return the index their
/// admin service swaps on every content write — so publishing a campaign takes effect on the next
/// event with no invalidation to get wrong. Three hash lookups on the calling thread is already the
/// budget the previous gate worked to.
/// </para>
/// <para>
/// With no source registered nothing is interesting, and every translator stops at the gate. That is
/// the correct answer, not a failure mode: a hotel with no progression content should pay nothing.
/// </para>
/// </remarks>
public sealed class SignalInterest(IEnumerable<ISignalInterestSource> sources) : ISignalInterest
{
    private readonly ImmutableArray<ISignalInterestSource> _sources = [.. sources];

    public bool AnyConsumerCares(string action)
    {
        foreach (ISignalInterestSource source in _sources)
        {
            if (source.Actions.Contains(action))
            {
                return true;
            }
        }

        return false;
    }
}
