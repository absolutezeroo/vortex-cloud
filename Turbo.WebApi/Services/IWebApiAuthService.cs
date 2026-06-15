using System.Threading;
using System.Threading.Tasks;

namespace Turbo.WebApi.Services;

public interface IWebApiAuthService
{
    Task<(bool Success, string? SessionId, string? Error)> LoginAsync(
        string email,
        string password,
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
