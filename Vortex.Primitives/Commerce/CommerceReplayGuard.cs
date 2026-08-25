using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Commerce;

/// <summary>
/// Whether a relayed event has reached a given consumer before.
/// </summary>
/// <remarks>
/// The commerce relay is at-least-once by construction: an operation writes its event with its
/// terminal transition and publishes it afterwards, so a crash in between means the sweep publishes
/// it later — and a publish that partly succeeded means some consumers see it twice. Consumers that
/// only read are free to. Consumers that change player state have to know, and this is how they ask.
/// </remarks>
public static class CommerceReplayGuard
{
    /// <summary>
    /// True when this is the first time <paramref name="consumer"/> has seen the event of
    /// <paramref name="operationId"/>, and false when it has seen it before.
    /// </summary>
    /// <remarks>
    /// An empty operation id means the event was raised outside a commerce operation and is never
    /// replayed, so it always passes. Each consumer passes its own name: two consumers of one event
    /// are two deliveries to deduplicate, and a shared key would let whichever ran first silence the
    /// other.
    /// </remarks>
    public static async ValueTask<bool> FirstDeliveryAsync(
        ICommerceJournal journal,
        string operationId,
        string consumer,
        CancellationToken ct
    )
    {
        if (string.IsNullOrEmpty(operationId) || !Guid.TryParse(operationId, out Guid parsed))
        {
            return true;
        }

        return await journal
            .TryRecordStepAsync(
                new CommerceOperationId(parsed),
                $"{CommerceStepKeys.RELAY}:{consumer}",
                null,
                ct
            )
            .ConfigureAwait(false);
    }
}
