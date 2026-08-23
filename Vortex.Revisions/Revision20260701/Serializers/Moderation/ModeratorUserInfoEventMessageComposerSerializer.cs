using Vortex.Protocol.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorUserInfoEventMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorUserInfoEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ModeratorUserInfoEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.UserId)
            .WriteString(message.UserName)
            .WriteString(message.Figure)
            .WriteInteger(message.RegistrationAgeInMinutes)
            .WriteInteger(message.MinutesSinceLastLogin)
            .WriteBoolean(message.Online)
            .WriteInteger(message.CfhCount)
            .WriteInteger(message.AbusiveCfhCount)
            .WriteInteger(message.CautionCount)
            .WriteInteger(message.BanCount)
            .WriteInteger(message.TradingLockCount)
            .WriteString(message.TradingExpiryDate)
            .WriteString(message.LastPurchaseDate)
            .WriteInteger(message.IdentityId)
            .WriteInteger(message.IdentityRelatedBanCount)
            .WriteString(message.PrimaryEmailAddress)
            .WriteString(message.UserClassification);

        // Optional tail: the client reads these two only while bytes remain, so they go out as a
        // pair or not at all.
        if (!message.HasSanctionHistory)
        {
            return;
        }

        packet.WriteString(message.LastSanctionTime).WriteInteger(message.SanctionAgeHours);
    }
}
