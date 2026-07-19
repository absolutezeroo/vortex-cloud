using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Orleans.Observers;

public interface ISessionContextObserver : IGrainObserver
{
    public Task SendComposerAsync(IComposer composer, CancellationToken ct = default);
}
