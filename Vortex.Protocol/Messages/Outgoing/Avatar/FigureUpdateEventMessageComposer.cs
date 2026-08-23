using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Protocol.Messages.Outgoing.Avatar;

[GenerateSerializer, Immutable]
public sealed record FigureUpdateEventMessageComposer : IComposer
{
    [Id(0)]
    public required string Figure { get; init; }

    [Id(1)]
    public required AvatarGenderType Gender { get; init; }
}
