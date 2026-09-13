using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Authentication;

/// <summary>
/// The places an account has already answered its security questions from, so the challenge is not
/// put again from the same one. habbo.com's "lieux de connexion autorisés".
/// </summary>
/// <remarks>
/// A place is a <c>fingerprint</c> — a keyed SHA-256 over the caller's address and user agent —
/// built by <see cref="Fingerprint" /> so every caller derives it the same way. The address itself
/// is never stored: see <c>PlayerAccountTrustedLocationEntity</c>.
/// </remarks>
public interface IAccountTrustedLocationService
{
    /// <summary>
    /// The fingerprint for a request. Keyed with <c>Vortex:Authentication:IpHashSecret</c>, so the
    /// stored value is useless to anyone who does not hold that secret — and rotating it drops every
    /// trusted location, which is the intended effect of rotating it.
    /// </summary>
    string Fingerprint(string? address, string? userAgent);

    Task<bool> IsTrustedAsync(int accountId, string fingerprint, CancellationToken ct = default);

    /// <summary>
    /// Remembers a place, or refreshes the one already there. Idempotent: the unique index on
    /// (account, fingerprint) is what makes a second call an update rather than a duplicate.
    /// </summary>
    Task TrustAsync(int accountId, string fingerprint, CancellationToken ct = default);

    /// <summary>Forgets every place, and answers how many there were.</summary>
    Task<int> ResetAsync(int accountId, CancellationToken ct = default);
}
