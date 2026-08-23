using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>The player closing a wired trade without completing it. No payload.</summary>
public record WiredTradeCancelMessage : IMessageEvent;
