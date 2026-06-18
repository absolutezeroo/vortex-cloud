using System.Threading;
using System.Threading.Tasks;

namespace Turbo.Primitives.Authentication;

/// <summary>
/// Verifies account credentials (email + password). Lives in <c>Turbo.Primitives</c> so any module
/// (game web API, admin dashboard, ...) can authenticate accounts without referencing the auth
/// runtime or a password-hashing library directly.
/// </summary>
public interface IAccountAuthenticator
{
    /// <summary>
    /// Returns the account id when the credentials are valid, otherwise <c>null</c>. Implementations
    /// must be constant-time with respect to whether the account exists.
    /// </summary>
    Task<int?> VerifyCredentialsAsync(
        string email,
        string password,
        CancellationToken ct = default
    );
}
