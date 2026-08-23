using Vortex.Protocol.Messages.Outgoing.Tracking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Tracking;
using Vortex.Revisions.Revision20260701.Serializers.Tracking;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class TrackingMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.EventLogMessageEvent, new EventLogMessageParser());
        builder.MapParser(
            MessageEvent.LagWarningReportMessageEvent,
            new LagWarningReportMessageParser()
        );
        builder.MapParser(
            MessageEvent.LatencyPingReportMessageEvent,
            new LatencyPingReportMessageParser()
        );
        builder.MapParser(
            MessageEvent.LatencyPingRequestMessageEvent,
            new LatencyPingRequestMessageParser()
        );
        builder.MapParser(
            MessageEvent.PerformanceLogMessageEvent,
            new PerformanceLogMessageParser()
        );

        builder.MapSerializer(
            typeof(LatencyPingResponseMessage),
            new LatencyPingResponseMessageSerializer(
                MessageComposer.LatencyPingResponseMessageComposer
            )
        );
    }
}
