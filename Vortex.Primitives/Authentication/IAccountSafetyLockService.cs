using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Authentication;

/// <summary>What stopped the safety lock from moving, or nothing.</summary>
public enum SafetyLockOutcome
{
    Succeeded,
    UnknownAccount,

    /// <summary>The current password was wrong.</summary>
    WrongPassword,

    /// <summary>The account has a second factor and no code came with the request.</summary>
    MfaRequired,

    /// <summary>A code came, and it was not valid.</summary>
    InvalidCode,
}

public readonly record struct SafetyLockResult(SafetyLockOutcome Outcome)
{
    public bool Succeeded => Outcome == SafetyLockOutcome.Succeeded;

    public static SafetyLockResult Failed(SafetyLockOutcome outcome) => new(outcome);

    public static SafetyLockResult Success() => new(SafetyLockOutcome.Succeeded);
}

/// <summary>
/// The account safety lock: while it is on, the account cannot spend — no catalog purchase, no
/// marketplace.
/// </summary>
/// <remarks>
/// <para>
/// It is what a player reaches for when they believe someone else is in their account. The thief
/// holds the session; the lock needs the password — and the second factor, when there is one — so
/// the credits stay put until the owner sorts it out. Both directions are verified, not just the
/// unlock: a thief who could lock the account would be a way to grief its owner.
/// </para>
/// <para>
/// habbo.com puts security questions on this instead. They are not reproduced: a question is a
/// second secret to store, weaker than a password and typically guessable by whoever knew the player
/// well enough to be in their account — and this hotel already has a stronger one in the second
/// factor.
/// </para>
/// <para>
/// Setting it takes effect at once on any connected avatar of the account, which is the case that
/// matters: a lock that waited for the next login would leave the thief spending in the meantime.
/// </para>
/// </remarks>
public interface IAccountSafetyLockService
{
    Task<bool?> IsLockedAsync(int accountId, CancellationToken ct = default);

    Task<SafetyLockResult> SetAsync(
        int accountId,
        bool locked,
        string currentPassword,
        string? code,
        CancellationToken ct = default
    );
}
