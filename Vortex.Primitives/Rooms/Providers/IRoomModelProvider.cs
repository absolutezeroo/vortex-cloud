using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Snapshots.Mapping;

namespace Vortex.Primitives.Rooms.Providers;

public interface IRoomModelProvider
{
    public RoomModelSnapshot GetModelById(int modelId);
    public Task ReloadAsync(CancellationToken ct = default);
}
