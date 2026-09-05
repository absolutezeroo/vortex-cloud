using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.RewardTracks;

namespace Vortex.Revisions.Revision20260701.Serializers.RewardTracks;

/// <summary>The client's <c>_SafeCls_3769.parse</c>: trackId, taskId, progressCount, points.</summary>
internal class RewardTrackProgressMessageComposerSerializer(int header)
    : AbstractSerializer<RewardTrackProgressMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RewardTrackProgressMessageComposer message
    ) =>
        packet
            .WriteString(message.TrackId)
            .WriteString(message.TaskId)
            .WriteInteger(message.ProgressCount)
            .WriteInteger(message.Points);
}
