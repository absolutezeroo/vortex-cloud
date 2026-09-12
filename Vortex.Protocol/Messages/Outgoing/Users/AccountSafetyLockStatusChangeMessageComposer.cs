using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

/// <summary>
/// The account safety lock was engaged or released (header 3913).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1891/_SafeCls_2001.as): a single int.
/// </summary>
/// <remarks>
/// <para>
/// <b>0 is LOCKED.</b> The comment here used to claim the opposite, which never bit only because
/// nothing had ever sent this message. The client settles it twice over:
/// <c>SessionDataManager.as:619</c> reads <c>_accountSafetyLocked = _loc2_.status == 0</c>, and the
/// notifications handler hides the "account locked" bubble when the status is 1.
/// </para>
/// <para>
/// Build it with <see cref="For"/> rather than writing the number: an inverted safety lock unlocks a
/// compromised account instead of locking it, and nothing downstream would say so.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record AccountSafetyLockStatusChangeMessageComposer : IComposer
{
    /// <summary>The wire value for a locked account.</summary>
    public const int LOCKED = 0;

    /// <summary>The wire value for an account that is not locked.</summary>
    public const int UNLOCKED = 1;

    [Id(0)]
    public required int Status { get; init; }

    public static AccountSafetyLockStatusChangeMessageComposer For(bool locked) =>
        new() { Status = locked ? LOCKED : UNLOCKED };
}
