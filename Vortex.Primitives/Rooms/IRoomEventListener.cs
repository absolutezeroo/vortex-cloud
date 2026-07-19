using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Events;

namespace Vortex.Primitives.Rooms;

public interface IRoomEventListener
{
    public Task OnRoomEventAsync(RoomEvent evt, CancellationToken ct);
}
