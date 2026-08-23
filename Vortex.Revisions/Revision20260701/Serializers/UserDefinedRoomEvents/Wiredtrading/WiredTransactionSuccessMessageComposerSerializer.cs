using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The reward notification a completed contract raises.
/// </summary>
/// <remarks>
/// The block after the type is written only for a reward contract that promises something. The
/// client reads it on the type and on there being bytes left, so an empty tail is legal — but a
/// tail under any other type would be read as the next message.
/// </remarks>
internal class WiredTransactionSuccessMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTransactionSuccessMessageComposer>(header)
{
    /// <summary>The one type whose payload carries a reward block.</summary>
    private const int RewardTransaction = 2;

    protected override void Serialize(
        IServerPacket packet,
        WiredTransactionSuccessMessageComposer message
    )
    {
        packet.WriteInteger(message.TransactionSuccessTypeId);

        if (message.TransactionSuccessTypeId != RewardTransaction || message.Reward is null)
        {
            return;
        }

        packet
            .WriteRule(message.Reward)
            .WriteString(message.RewardText)
            .WriteBoolean(message.OpenByDefault);
    }
}
