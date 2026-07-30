using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Mysterybox;

/// <summary>
/// The prize won when a mystery box is opened. <see cref="ContentType"/> is the legacy product code
/// the client switches on to draw the reward icon -- "s" (floor furni), "i" (wall furni),
/// "e" (avatar effect) or "h" (club product); any other value leaves the reward window blank.
/// <see cref="ClassId"/> is the furni sprite id for "s"/"i", the effect id for "e".
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GotMysteryBoxPrizeMessageComposer : IComposer
{
    [Id(0)]
    public required string ContentType { get; init; }

    [Id(1)]
    public required int ClassId { get; init; }
}
