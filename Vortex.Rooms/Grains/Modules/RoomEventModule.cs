using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
            // Per-listener, because the list is no longer only the room's own systems: assemblies
            // scanned at startup contribute listeners too, and a throw used to abandon every
            // listener after it and surface inside whatever gameplay path raised the event.
            try
            {
                await listener.OnRoomEventAsync(evt, ct);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Room event listener {Listener} failed on {Event} in room {RoomId}",
                    listener.GetType().FullName,
                    evt.GetType().Name,
                    _roomGrain.RoomId.Value
                );
            }
        }
    }
}
