using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Roomsettings;

/// <summary>
/// The room's silence switch, echoed back after it is toggled. The client stores it on the room
/// data as <c>allInRoomMuted</c> (navigator/_SafeCls_1951.as:178) and that is what redraws the
/// "mute all" button in the room-info panel.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record MuteAllInRoomEventMessageComposer : IComposer
{
    [Id(0)]
    public required bool AllMuted { get; init; }
}
