using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Habbicons;

/// <summary>
/// The Habbicon hub asking for the whole shop (<c>HabbiconController.getShopData</c>). No payload.
/// </summary>
public sealed record GetHabbiconShopDataMessage : IMessageEvent;
