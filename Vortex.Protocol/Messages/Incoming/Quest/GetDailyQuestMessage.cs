using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Quest;

public record GetDailyQuestMessage : IMessageEvent
{
    /// <summary>The widget sends true on a full refresh (DailyQuestWidget.as:99-103), where it also
    /// resets its index to 0. Read so the wire stays in sync; nothing consumes it yet.</summary>
    public bool Refresh { get; init; }

    /// <summary>The quest index the widget is asking for, reset to 0 by a refresh.</summary>
    public int Index { get; init; }
}
