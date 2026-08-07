using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Help;

/// <summary>
/// The guide asking the person they are helping to come to them. The name travels with the id
/// because the client offers the invitation before following it, and has nothing else to name the
/// room with.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuideSessionInvitedToGuideRoomMessageComposer : IComposer
{
    [Id(0)]
    public required int RoomId { get; init; }

    [Id(1)]
    public required string RoomName { get; init; }
}
