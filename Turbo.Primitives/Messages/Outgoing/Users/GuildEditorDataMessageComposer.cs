using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildEditorDataMessageComposer : IComposer
{
    [Id(0)]
    public required GroupEditorDataSnapshot Data { get; init; }
}
