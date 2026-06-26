using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record PetSupplementedNotificationEventMessageComposer : IComposer
{
    [Id(0)]
    public required int PetId { get; init; }

    [Id(1)]
    public required int UserId { get; init; }

    /// <summary>2 = revive, 3 = rebreed fertilize, 4 = speed fertilize</summary>
    [Id(2)]
    public required int SupplementType { get; init; }
}
