using Orleans;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Chat;

/// <summary>
/// The room's chat settings, live. The client already receives them once inside GuestRoomData when
/// you enter; this is how it hears about a change without leaving and coming back.
///
/// Only the flood sensitivity is on the wire in this revision: the client's
/// <c>fromFloodSensitivity</c> builds the rest of the settings object from constants, and takes the
/// bubble width, scroll speed and chat mode from the player's own account preferences instead.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomChatSettingsMessageComposer : IComposer
{
    [Id(0)]
    public required ChatFloodSensitivityType FloodSensitivity { get; init; }
}
