using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Primitives.Authentication;

namespace Turbo.Authentication;

/// <summary>
/// Verifies account credentials against the <c>player_accounts</c> table using BCrypt. Password
/// verification is CPU-bound, so it runs off the calling thread. When the account is missing a dummy
/// BCrypt verification is still performed so the response time does not reveal account existence.
/// </summary>
internal sealed class AccountAuthenticator(IDbContextFactory<TurboDbContext> dbContextFactory)
    : IAccountAuthenticator
{
    // A pre-computed BCrypt hash of a random value, used to keep timing constant when no account
    // matches the supplied email (prevents user-enumeration via response timing).
    private const string DummyHash = "$2a$12$C6UzMDM.H6dfI/f/IKcEeO3qj8b1l1u8j0Y9o6m4w8h2tY6q0Q1Qe";

    public async Task<int?> VerifyCredentialsAsync(
        string email,
        string password,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        await using var dbCtx = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        var account = await dbCtx
            .PlayerAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == normalizedEmail, ct)
            .ConfigureAwait(false);

        var hash = account?.PasswordHash ?? DummyHash;

        var valid = await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, hash), ct)
            .ConfigureAwait(false);

        return valid && account is not null ? account.Id : null;
    }
}
