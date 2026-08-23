using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Quest;

/// <summary>
/// "Open the quest window on this campaign." Sent by the quest tracker with the client's default
/// campaign, and by the citizenship/VIP toolbar promo with its own campaign name.
/// </summary>
public record StartCampaignMessage : IMessageEvent
{
    public required string CampaignCode { get; init; }
}
