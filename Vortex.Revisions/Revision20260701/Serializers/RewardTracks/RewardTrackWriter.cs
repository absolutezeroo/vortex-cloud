using Vortex.Primitives.Packets;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.RewardTracks;

/// <summary>
/// The reward-track blocks, matching the client's <c>_SafeCls_2628</c> (track), <c>_SafeCls_4299</c>
/// (task), <c>_SafeCls_4391</c> (task level) and <c>_SafeCls_4204</c> (prize) constructors field for
/// field.
/// </summary>
internal static class RewardTrackWriter
{
    /// <summary>
    /// One whole track.
    /// </summary>
    /// <remarks>
    /// The premium block is <em>conditional</em>: the client reads <c>hasPremiumConfig</c> and only
    /// then reads the four premium fields. Writing them unconditionally misaligns every field after
    /// them, and the first thing that goes wrong is the task count being read out of the boost's
    /// bytes — so a track with no premium tier must write the boolean and stop.
    /// </remarks>
    public static void WriteTrack(IServerPacket packet, RewardTrackViewSnapshot track)
    {
        packet
            .WriteString(track.TrackId)
            .WriteString(track.Theme)
            .WriteInteger(track.Points)
            .WriteBoolean(track.Premium is not null);

        if (track.Premium is RewardTrackPremiumSnapshot premium)
        {
            packet
                .WriteDouble(premium.BoostMultiplier)
                .WriteInteger(premium.InstantPoints)
                .WriteInteger(premium.CostDiamonds)
                .WriteInteger(premium.CostCredits);
        }

        packet
            .WriteBoolean(track.PremiumUnlocked)
            .WriteBoolean(track.Complete)
            .WriteBoolean(track.PremiumComplete)
            .WriteInteger(track.Tasks.Length);

        foreach (RewardTrackTaskViewSnapshot task in track.Tasks)
        {
            WriteTask(packet, task);
        }

        packet.WriteInteger(track.Prizes.Length);

        foreach (RewardTrackPrizeViewSnapshot prize in track.Prizes)
        {
            WritePrize(packet, prize);
        }
    }

    /// <summary>
    /// One task: id, actionType, parameter, progressCount, premium, then a count and that many
    /// levels.
    /// </summary>
    private static void WriteTask(IServerPacket packet, RewardTrackTaskViewSnapshot task)
    {
        packet
            .WriteString(task.TaskId)
            .WriteString(task.ActionCode)
            .WriteString(task.Parameter)
            .WriteInteger(task.ProgressCount)
            .WriteBoolean(task.Premium)
            .WriteInteger(task.Levels.Length);

        foreach (RewardTrackTaskLevelSnapshot level in task.Levels)
        {
            packet
                .WriteInteger(level.RequiredCount)
                .WriteInteger(level.PointsReward)
                .WriteBoolean(level.Premium);
        }
    }

    /// <summary>
    /// One prize. <c>productItemTypeId</c> is a <em>short</em> here while every other numeric field
    /// on the block is an int — the client reads it with <c>readShort</c>, and writing it as an int
    /// shifts the rest of the block by two bytes.
    /// </summary>
    private static void WritePrize(IServerPacket packet, RewardTrackPrizeViewSnapshot prize) =>
        packet
            .WriteString(prize.PrizeId)
            .WriteInteger(prize.RequiredPoints)
            .WriteShort((short)prize.Kind)
            .WriteString(prize.RewardTypeId)
            .WriteString(prize.ExtraParams)
            .WriteInteger(prize.RewardAmount)
            .WriteBoolean(prize.Premium)
            .WriteBoolean(prize.Available)
            .WriteBoolean(prize.Claimed);
}
