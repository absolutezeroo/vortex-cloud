using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>The player closing a wired trade without completing it. No payload.</summary>
public record WiredTradeCancelMessage : IMessageEvent;
