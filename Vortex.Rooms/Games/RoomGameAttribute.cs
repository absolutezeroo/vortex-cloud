using System;

namespace Vortex.Rooms.Games;

/// <summary>
/// Marks an <see cref="Abstractions.IRoomGame"/> that should be built and plugged into every room.
/// This attribute is the entire registration story for a game: there is no list to append to, no
/// line to add to the room grain's constructor, and nothing that starts or stops a round has to
/// learn the game's name.
/// <para>
/// It is the attribute rather than the interface that registers, for the same reason
/// <c>[RoomEventListener]</c> works that way: a test double implements the interface too, and
/// scanning the interface alone would put every stand-in game into every room.
/// </para>
/// <para>
/// The type is constructed once per room through <c>ActivatorUtilities</c>, with its
/// <see cref="Abstractions.IRoomGameContext"/> as the first argument and any further constructor
/// parameters resolved from the container.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RoomGameAttribute : Attribute;
