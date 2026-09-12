using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Authentication;

/// <summary>What stopped an address from changing, or nothing.</summary>
public enum EmailChangeOutcome
{
    Succeeded,
    UnknownAccount,

    /// <summary>The current password was wrong.</summary>
    WrongPassword,

    /// <summary>The account has a second factor and no code came with the request.</summary>
    MfaRequired,

    /// <summary>A code came, and it was not valid.</summary>
    InvalidCode,

    /// <summary>The new address is not shaped like an address.</summary>
    Invalid,

    /// <summary>Another account already signs in with it.</summary>
    Taken,
}

public readonly record struct EmailChangeResult(EmailChangeOutcome Outcome)
{
    public bool Succeeded => Outcome == EmailChangeOutcome.Succeeded;

    public static EmailChangeResult Failed(EmailChangeOutcome outcome) => new(outcome);

    public static EmailChangeResult Success() => new(EmailChangeOutcome.Succeeded);
}

/// <summary>
/// Changes the address an account signs in with.
/// </summary>
/// <remarks>
/// <para>
/// Re-authentication goes through <see cref="IAccountAuthenticator"/> rather than a hash comparison
/// of its own, exactly as <see cref="IAccountPasswordService"/> does — which is also how the second
/// factor comes along for free: an account that has one cannot have its address moved by a session
/// alone, and a stolen cookie is not enough to take the account over.
/// </para>
/// <para>
/// The new address is NOT verified, because this hotel has no way to send to it: there is no SMTP
/// path, no queue and no verification token anywhere in the server. A player can therefore set an
/// address they do not own, and would then have to remember their password — it is the login
/// identifier, not a recovery channel. Wiring delivery is what turns that around, and until it
/// exists the website says so rather than showing a dead "resend verification" button.
/// </para>
/// </remarks>
public interface IAccountEmailService
{
    /// <summary>The address an account signs in with, or null when there is no such account.</summary>
    Task<string?> GetAsync(int accountId, CancellationToken ct = default);

    Task<EmailChangeResult> ChangeAsync(
        int accountId,
        string currentPassword,
        string newEmail,
        string? code,
        CancellationToken ct = default
    );
}
