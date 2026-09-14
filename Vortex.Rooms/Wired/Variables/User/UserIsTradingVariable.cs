using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// Whether the avatar has a trade window open. Read from the room's trade sessions rather than from
/// an avatar status, because a trade in this codebase IS the session — nothing sets a
/// <c>Trading</c> status, so a status test would have answered false for every real trade.
/// </summary>
public sealed class UserIsTradingVariable(RoomGrain roomGrain) : UserPlayerVariable(roomGrain)
{
    protected override string VariableName => "@is_trading";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 130;
    protected override WiredVariableFlags Flags => WiredVariableFlags.None;

    protected override bool TryGetValueForAvatar(IRoomPlayer avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        return _roomGrain._state.TradeSessionsByPlayerId.ContainsKey(avatar.PlayerId);
    }
}
