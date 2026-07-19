using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Networking;

public interface INetworkManager
{
    public Task StartAsync(CancellationToken ct);
    public Task StopAsync();
}
