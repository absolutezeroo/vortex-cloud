using System.Threading;
using System.Threading.Tasks;

namespace Vortex.WebApi.Services;

public interface IWebApiAuthService
{
    /// <summary>
    /// Authenticates an account and opens a web session. <paramref name="code" /> is the second
    /// factor and is null on a first attempt; an account that has one answers
    /// <c>pocket.auth.mfa_required</c> and the client resubmits with the code.
    /// </summary>
    Task<(bool Success, string? SessionId, int AccountId, string? Error)> LoginAsync(
        string email,
        string password,
        string? code,
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
