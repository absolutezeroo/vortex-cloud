using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms.RaidProtection;

namespace Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;

/// <summary>
/// The answer to a save.
/// </summary>
/// <remarks>
/// <para>
/// Two things the client does with it decide the contract. It ignores a reply whose room is not the
/// one it has a save outstanding for — so the room id has to be the saved room's, never the
/// player's current one. And on a non-zero <see cref="ResultCode" /> it leaves the panel open and
/// repaints every control from <see cref="Settings" />, so the settings carried here are always the
/// room's real state, never the draft that was refused.
/// </para>
/// <para>
/// On the wire the room id is written once, ahead of the code, and the snapshot's remaining nine
/// fields follow (<c>_SafeCls_4512.parse</c>, AIR 1.0.31).
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record RaidProtectionSettingsResultMessageComposer : IComposer
{
    /// <summary>
    /// The outcome. Typed, not a raw int: the client declares exactly seven codes and would carry a
    /// value outside that range straight through to a branch with nothing to match it, so the way
    /// to keep the wire honest is to make the out-of-range value unsayable rather than to check for
    /// it on the way out.
    /// </summary>
    [Id(0)]
    public required RaidProtectionSaveResult Result { get; init; }

    [Id(1)]
    public required RoomRaidProtectionSnapshot Settings { get; init; }
}
