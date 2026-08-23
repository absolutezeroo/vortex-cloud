using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Bots;

/// <summary>Why a bot action was refused. The client maps the code to its own localised string, so
/// the value has to be one it knows rather than anything the server invents.</summary>
[GenerateSerializer, Immutable]
public sealed record BotErrorMessageComposer : IComposer
{
    [Id(0)]
    public required int ErrorCode { get; init; }
}
