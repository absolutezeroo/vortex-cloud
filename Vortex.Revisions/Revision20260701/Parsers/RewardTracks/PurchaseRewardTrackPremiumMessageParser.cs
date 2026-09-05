using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.RewardTracks;

namespace Vortex.Revisions.Revision20260701.Parsers.RewardTracks;

internal class PurchaseRewardTrackPremiumMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PurchaseRewardTrackPremiumMessage { TrackId = packet.PopString() };
}
