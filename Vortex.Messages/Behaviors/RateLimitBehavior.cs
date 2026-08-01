using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Messages.RateLimit;
using Vortex.Messages.Registry;
using Vortex.Pipeline.Attributes;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;

namespace Vortex.Messages.Behaviors;

/// <summary>
/// Global per-session token-bucket gate on every inbound message (SEC-03). Registered for
/// <see cref="IMessageEvent"/> itself, so <c>EnableInheritanceDispatch</c> applies it to every
/// concrete message type without listing them; ordered to run before any other behavior or handler
/// so a rate-limited packet never reaches business logic (or the extra grain call that resolving
/// its context would otherwise cost).
/// </summary>
[Order(int.MinValue)]
public sealed class RateLimitBehavior(
    IRateLimiter limiter,
    IVortexMetrics metrics,
    ILogger<RateLimitBehavior> logger
) : IMessageBehavior<IMessageEvent>
{
    public ValueTask InvokeAsync(
        IMessageEvent env,
        MessageContext ctx,
        Func<ValueTask> next,
        CancellationToken ct
    )
    {
        if (limiter.TryConsume(ctx.SessionKey))
        {
            return next();
        }

        metrics.PacketDropped("rate_limited");

        logger.LogDebug(
            "Rate limit exceeded for session {SessionKey}; dropping {MessageType}.",
            ctx.SessionKey,
            env.GetType().Name
        );

        return ValueTask.CompletedTask;
    }
}
