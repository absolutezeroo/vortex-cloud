using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Events;

namespace Vortex.Rooms.Grains.Modules;

public sealed class RoomEventModule(RoomGrain roomGrain)
{
    private readonly List<IRoomEventListener> _listeners = [];
    private readonly RoomGrain _roomGrain = roomGrain;

    public void Register(IRoomEventListener listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    public void Unregister(IRoomEventListener listener)
    {
        _listeners.Remove(listener);
    }

    public async Task PublishAsync(RoomEvent evt, CancellationToken ct)
    {
        foreach (IRoomEventListener listener in _listeners)
        {
            await listener.OnRoomEventAsync(evt, ct);
        }
    }
}
