namespace Vortex.Primitives.Rooms.Events;

/// <summary>
/// The cause a pile carries when another pile executed it through the "execute stacks" action,
/// rather than one of its own triggers.
/// </summary>
/// <remarks>
/// It is never published to the room: a called pile has no trigger to answer an event, and
/// publishing this would invite exactly the recursion the call chain guards against. It exists so
/// the processing context of a called pile has an honest cause instead of borrowing the caller's.
/// </remarks>
public sealed record WiredStackCalledEvent : RoomEvent;
