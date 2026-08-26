using System;
using System.Threading;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Security;

/// <summary>
///     Who is asking, as the server knows it, for the duration of one dashboard request.
/// </summary>
/// <remarks>
///     <para>
///     The frozen note's rule is that a privileged operation must not depend on a bare
///     <c>string actor</c> as its only security context. That string is still there and still right
///     for what it does — it is the name on the audit row — but it is the <em>caller's</em> word, and
///     an operation that wanted to make a decision of its own had nothing else to go on.
///     </para>
///     <para>
///     This is the rest of it: the account behind the email, the session the request arrived on, the
///     capabilities the server resolved (not the ones the browser claims), when that session last
///     proved a second factor, and the correlation id everything else is stamped with. Ambient rather
///     than threaded through ninety-five method signatures, for the same reason the correlation id is:
///     the value belongs to the request, not to any one call inside it, and a parameter every method
///     forwards untouched is a parameter somebody eventually forgets to forward.
///     </para>
///     <para>
///     <see cref="Current" /> is null outside a request — a console command, a background sweep, a
///     test. That is a real state and not an error, so every reader has to handle it rather than
///     assume a caller.
///     </para>
/// </remarks>
internal sealed record ActorSecurityContext
{
    private static readonly AsyncLocal<ActorSecurityContext?> Ambient = new();

    /// <summary>The operator behind the request being served, or null when there is no request.</summary>
    public static ActorSecurityContext? Current => Ambient.Value;

    public required int AccountId { get; init; }

    public required string Email { get; init; }

    /// <summary>The session cookie's opaque token. Identifies the window, not the person.</summary>
    public required string? SessionId { get; init; }

    /// <summary>
    ///     What the server resolved this account may do, re-read on every request so a revoked role
    ///     takes effect immediately. Never what the browser said.
    /// </summary>
    public required PermissionSet Permissions { get; init; }

    /// <summary>When this session last proved a second factor, or null if it never has.</summary>
    public required DateTime? SteppedUpAtUtc { get; init; }

    public required CorrelationId CorrelationId { get; init; }

    public bool Has(string capability) => Permissions.Has(capability);

    /// <summary>Makes this the ambient context until the returned scope is disposed.</summary>
    public static IDisposable Enter(ActorSecurityContext context)
    {
        ActorSecurityContext? previous = Ambient.Value;

        Ambient.Value = context;

        return new Scope(previous);
    }

    private sealed class Scope(ActorSecurityContext? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Ambient.Value = previous;
        }
    }
}
