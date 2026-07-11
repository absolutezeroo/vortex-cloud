using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Users;

internal class ScrSendKickbackInfoMessageComposerSerializer(int header)
    : AbstractSerializer<ScrSendKickbackInfoMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ScrSendKickbackInfoMessageComposer message
    )
    {
        packet.WriteInteger(message.CurrentHcStreak);
        packet.WriteString(message.FirstSubscriptionDate);
        packet.WriteDouble(message.KickbackPercentage);
        packet.WriteInteger(message.TotalCreditsMissed);
        packet.WriteInteger(message.TotalCreditsRewarded);
        packet.WriteInteger(message.TotalCreditsSpent);
        packet.WriteInteger(message.CreditRewardForStreakBonus);
        packet.WriteInteger(message.CreditRewardForMonthlySpent);
        packet.WriteInteger(message.TimeUntilPayday);
    }
}
