using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Authentication;

public interface IAuthenticationService
{
    public Task<int> GetPlayerIdFromTicketAsync(
        string ticket,
        string? remoteIp = null,
        CancellationToken ct = default
    );
}
