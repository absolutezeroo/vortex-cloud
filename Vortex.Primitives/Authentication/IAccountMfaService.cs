using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Authentication;

/// <summary>
/// Second-factor enrolment and verification for an account. The dashboard cookie is worth the whole
/// hotel -- the currency, the bans, the staff roster, the server console -- and until this existed a
/// password was the only thing between someone and all of it.
///
/// <para>
/// Enrolment is deliberately two steps. <see cref="BeginEnrolmentAsync" /> hands back a secret and
/// stores nothing; only <see cref="ConfirmEnrolmentAsync" />, which requires a code computed from
/// that secret, writes it. An operator whose authenticator never actually took the secret therefore
/// cannot lock themselves out by walking away from the dialog.
/// </para>
/// </summary>
public interface IAccountMfaService
{
    /// <summary>Whether the account has a confirmed second factor.</summary>
    Task<bool> IsEnabledAsync(int accountId, CancellationToken ct = default);

    /// <summary>
    /// A fresh secret and the <c>otpauth://</c> URI for it. Nothing is stored: the caller shows both
    /// to the operator and hands the secret back to <see cref="ConfirmEnrolmentAsync" />.
    /// </summary>
    Task<MfaEnrolment> BeginEnrolmentAsync(int accountId, CancellationToken ct = default);

    /// <summary>
    /// Stores <paramref name="secret" /> as the account's second factor, but only if
    /// <paramref name="code" /> proves an authenticator already holds it. False means the code did
    /// not verify and nothing was written.
    ///
    /// <para>
    /// Enrolment only: an account that already has a factor is refused (also false). The code proves
    /// possession of the secret the caller just supplied, which says nothing about who is asking, so
    /// letting it overwrite would let a stolen session install its own factor. Replacing a factor
    /// means <see cref="DisableAsync" /> first, which demands a code from the one already stored.
    /// </para>
    /// </summary>
    Task<bool> ConfirmEnrolmentAsync(
        int accountId,
        string secret,
        string code,
        CancellationToken ct = default
    );

    /// <summary>Whether <paramref name="code" /> is currently valid for the account. An account with no second factor answers false.</summary>
    Task<bool> VerifyAsync(int accountId, string? code, CancellationToken ct = default);

    /// <summary>
    /// Removes the account's second factor. <paramref name="code" /> is required when an operator
    /// disables their own -- a hijacked session must not be able to take the factor off -- and is
    /// null when a staff administrator clears it for someone who lost their authenticator, which is
    /// the recovery path instead of a set of one-time codes nobody keeps.
    /// </summary>
    Task<bool> DisableAsync(int accountId, string? code, CancellationToken ct = default);
}

/// <summary>A secret and the URI an authenticator app reads, neither of them stored yet.</summary>
public readonly record struct MfaEnrolment(string Secret, string Uri);
