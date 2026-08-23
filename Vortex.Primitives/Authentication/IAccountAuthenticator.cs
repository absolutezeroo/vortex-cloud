using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Authentication;

/// <summary>
/// Verifies account credentials (email + password + second factor). Lives in
/// <c>Vortex.Primitives</c> so any module (game web API, admin dashboard, ...) can authenticate
/// accounts without referencing the auth runtime or a password-hashing library directly.
///
/// <para>
/// The second factor is part of this call rather than a separate one an caller could forget. It was
/// separate once: the dashboard checked it after verifying the password, and the web API -- which
/// re-implemented the password check instead of coming through here -- did not, so a factor enrolled
/// on an account protected the admin cookie while the same password still opened the site and the
/// SSO ticket into the game. One entry point is what makes that impossible rather than remembered.
/// </para>
/// </summary>
public interface IAccountAuthenticator
{
    /// <summary>
    /// Verifies the credentials and, when the account has one, the second factor.
    /// <paramref name="code" /> is null on a first attempt: an account with a factor answers
    /// <see cref="AccountVerificationOutcome.MfaRequired" /> and the caller asks for a code.
    /// Implementations must be constant-time with respect to whether the account exists.
    /// </summary>
    Task<AccountVerification> VerifyCredentialsAsync(
        string email,
        string password,
        string? code,
        CancellationToken ct = default
    );
}

/// <summary>Why a verification did or did not succeed.</summary>
public enum AccountVerificationOutcome
{
    /// <summary>No such account, or the wrong password. Never says which.</summary>
    InvalidCredentials,

    /// <summary>The password was right and the account has a factor the request did not carry.</summary>
    MfaRequired,

    /// <summary>The password was right, a code was supplied, and it did not verify.</summary>
    InvalidCode,

    /// <summary>Everything the account requires was supplied and checked.</summary>
    Verified,
}

/// <summary>
/// The outcome, and the account id when there is one. <see cref="AccountId" /> is only meaningful for
/// <see cref="AccountVerificationOutcome.Verified" /> -- the other three deliberately carry nothing,
/// so a caller cannot half-authenticate someone by reading an id off a failed attempt.
/// </summary>
public readonly record struct AccountVerification(AccountVerificationOutcome Outcome, int AccountId)
{
    public static AccountVerification InvalidCredentials { get; } =
        new(AccountVerificationOutcome.InvalidCredentials, 0);

    public static AccountVerification MfaRequired { get; } =
        new(AccountVerificationOutcome.MfaRequired, 0);

    public static AccountVerification InvalidCode { get; } =
        new(AccountVerificationOutcome.InvalidCode, 0);

    public static AccountVerification Verified(int accountId) =>
        new(AccountVerificationOutcome.Verified, accountId);

    public bool IsVerified => Outcome == AccountVerificationOutcome.Verified;
}
