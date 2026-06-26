using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetPlacingErrorMessageComposer : IComposer
{
    [Id(0)]
    public required int ErrorCode { get; init; }
}
