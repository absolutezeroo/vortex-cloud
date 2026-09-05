using Orleans;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Habbicons;

/// <summary>
/// One Habbicon's state changed for this player: acquired, claimed, favourited, revoked.
/// </summary>
/// <remarks>
/// The incremental counterpart to <see cref="UserHabbiconsMessageComposer"/>. Sending
/// <see cref="HabbiconState.NotOwned"/> is how a Habbicon is taken away -- the client deletes its
/// row for any state it does not consider stored.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record UserHabbiconStatusChangedMessageComposer : IComposer
{
    [Id(0)]
    public required int HabbiconId { get; init; }

    [Id(1)]
    public required HabbiconState State { get; init; }
}
