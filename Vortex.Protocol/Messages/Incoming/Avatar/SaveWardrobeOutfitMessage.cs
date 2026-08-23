using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Avatar;

public record SaveWardrobeOutfitMessage : IMessageEvent
{
    // SaveWardrobeOutfitMessageComposer(slotId, figure, gender) — header 116.
    public required int SlotId { get; init; }
    public required string Figure { get; init; }
    public required string Gender { get; init; }
}
