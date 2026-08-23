using System.Collections.Generic;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.NewNavigator;

namespace Vortex.Protocol.Messages.Outgoing.NewNavigator;

public sealed record NavigatorLiftedRoomsMessage : IComposer
{
    public required List<NavigatorLiftedRoomSnapshot> LiftedRooms { get; init; }
}
