using Vortex.Primitives.Messages.Incoming.Tracking;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Tracking;

internal class PerformanceLogMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PerformanceLogMessage
        {
            ElapsedTime = packet.PopInt(),
            UserAgent = packet.PopString(),
            FlashVersion = packet.PopString(),
            OS = packet.PopString(),
            Browser = packet.PopString(),
            IsDebugger = packet.PopBoolean(),
            MemoryUsage = packet.PopInt(),
            unknownField = packet.PopInt(),
            GarbageCollections = packet.PopInt(),
            AverageFrameRate = packet.PopInt(),
            SlowUpdates = packet.PopInt(),
        };
}
