using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.RewardTracks;

namespace Vortex.Revisions.Revision20260701.Parsers.RewardTracks;

internal class ClaimRewardTrackPrizeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ClaimRewardTrackPrizeMessage
        {
            TrackId = packet.PopString(),
            PrizeId = packet.PopString(),
        };
}
