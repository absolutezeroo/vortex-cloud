using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

/// <summary>
/// The override bounds sit behind one flag and are written as a pair or not at all — the client
/// reads both or neither, so writing one of them shifts every field after it.
/// </summary>
internal class VariableFxStatusUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<VariableFxStatusUpdateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VariableFxStatusUpdateMessageComposer message
    )
    {
        packet.WriteBoolean(message.ForceInitialize).WriteInteger(message.Statuses.Length);

        foreach (WiredVariableFxStatusSnapshot status in message.Statuses)
        {
            bool hasOverrides =
                status.OverrideMinValue is not null && status.OverrideMaxValue is not null;

            packet
                .WriteString(status.StatusKey)
                .WriteBoolean(status.IsInitialize)
                .WriteBoolean(status.IsUserEntity)
                .WriteInteger(status.EntityId)
                .WriteLong(status.Value)
                .WriteBoolean(hasOverrides);

            if (hasOverrides)
            {
                packet
                    .WriteLong(status.OverrideMinValue!.Value)
                    .WriteLong(status.OverrideMaxValue!.Value);
            }

            packet.WriteInteger(status.Extra.Length);

            foreach (WiredVariableFxExtra extra in status.Extra)
            {
                packet.WriteString(extra.Key).WriteString(extra.Value);
            }
        }
    }
}
