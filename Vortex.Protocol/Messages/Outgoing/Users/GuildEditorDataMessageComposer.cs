using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildEditorDataMessageComposer : IComposer
{
    [Id(0)]
    public required GroupEditorDataSnapshot Data { get; init; }
}
