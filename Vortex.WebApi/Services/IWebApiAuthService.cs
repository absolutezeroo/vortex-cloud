using System.Threading;
using System.Threading.Tasks;

namespace Vortex.WebApi.Services;

public interface IWebApiAuthService
{
    /// <summary>
    /// Authenticates an account and opens a web session. <paramref name="code" /> is the second
    /// factor and is null on a first attempt; an account that has one answers
    /// <c>pocket.auth.mfa_required</c> and the client resubmits with the code.
    ///
    /// <para>
    /// <paramref name="address" /> and <paramref name="userAgent" /> identify the PLACE the sign-in
    /// came from. An account with security questions that has never answered from here opens an
    /// UNTRUSTED session and has its safety lock armed — habbo.com's "si nous détectons que ton
    /// compte est en danger, ton compte sera verrouillé". Both are optional, and a caller that
    /// passes neither gets what this did before: a trusted session, always.
    /// </para>
    /// </summary>
    Task<(bool Success, string? SessionId, int AccountId, string? Error)> LoginAsync(
        string email,
        string password,
        string? code,
        string? address,
        string? userAgent,
        CancellationToken ct
    );

    Task<(bool Success, int AccountId, string? Error)> RegisterAsync(
        string email,
        string password,
        CancellationToken ct
    );

    Task<(bool Success, string? Ticket, string? Error)> GetSsoTokenAsync(
        int playerId,
        string ip,
        CancellationToken ct
    );
}
