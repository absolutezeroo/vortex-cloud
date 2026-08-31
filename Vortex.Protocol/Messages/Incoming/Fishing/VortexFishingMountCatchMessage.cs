using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Fishing;

/// <summary>
/// Turn one recorded catch into a mountable trophy. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// Names a record id the server itself issued in <c>CatchResult</c>, so the client chooses which of
/// its catches to mount and nothing about the trophy — the species, the weight and the engraving all
/// come from the stored row, not from the request.
/// </remarks>
public record VortexFishingMountCatchMessage : IMessageEvent
{
    public required int RecordId { get; init; }
}
