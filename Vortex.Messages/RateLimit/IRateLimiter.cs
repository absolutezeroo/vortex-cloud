using Vortex.Primitives.Networking;

namespace Vortex.Messages.RateLimit;

public interface IRateLimiter
{
    /// <summary>Attempts to consume one token for <paramref name="session"/>. Returns false when the
    /// session's bucket is empty and the packet should be dropped instead of dispatched.</summary>
    bool TryConsume(SessionKey session);
}
