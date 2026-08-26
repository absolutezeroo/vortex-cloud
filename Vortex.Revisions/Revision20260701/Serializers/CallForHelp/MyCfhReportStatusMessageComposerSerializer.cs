using Vortex.Primitives.Moderation;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Callforhelp;

namespace Vortex.Revisions.Revision20260701.Serializers.CallForHelp;

internal class MyCfhReportStatusMessageComposerSerializer(int header)
    : AbstractSerializer<MyCfhReportStatusMessageComposer>(header)
{
    /// <summary>The client's APPEAL_NONE. It gates the whole appeal half of the window: anything
    /// else and it prints an appeal date and outcome for an appeal that was never filed.</summary>
    private const byte AppealStatusNone = 0;

    protected override void Serialize(
        IServerPacket packet,
        MyCfhReportStatusMessageComposer message
    )
    {
        packet.WriteInteger(message.Reports.Length);

        foreach (CfhReportStatusSnapshot report in message.Reports)
        {
            packet
                // Read with readLong into an int on the far side, so it is eight bytes here.
                .WriteLong(report.Id)
                .WriteLong(report.CreationTime)
                .WriteString(report.Message)
                .WriteInteger(report.TopicId)
                .WriteString(report.ReportedAccountName)
                .WriteLong(report.CloseTime)
                .WriteBoolean(report.Sanctioned)
                // No auto-moderation in this hotel: every sanction on a report came from a
                // moderator closing it, which is the branch this false selects.
                .WriteBoolean(false)
                .WriteByte(AppealStatusNone)
                // Appeals are not implemented (the button's request, header 3028, is unmapped).
                // -1, not 0, for the same reason CloseTime uses it on an open report.
                .WriteLong(-1)
                .WriteLong(-1);
        }
    }
}
