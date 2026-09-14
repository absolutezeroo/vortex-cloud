using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// Whether the thing standing there is a player, a pet or a bot.
/// </summary>
/// <remarks>
/// Read-only, and it used to claim otherwise: it declared <c>CanWriteValue</c> with no write behind
/// it, which put it in the "change variable value" picker — that box filters on exactly that flag —
/// where a builder could select it and watch nothing happen. Nothing turns a player into a bot, so
/// the flag was the thing that was wrong.
/// </remarks>
public sealed class UserTypeVariable(RoomGrain roomGrain) : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override string VariableName => "@type";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 10;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.AlwaysAvailable
        | WiredVariableFlags.HasTextConnector;

    protected override Dictionary<WiredVariableValue, string> GetTextConnectors() =>
        Enum.GetValues<RoomObjectType>()
            .ToDictionary(
                v => WiredVariableValue.Parse((int)v),
                v => RoomObjectTypeExtensions.GetString(v)
            );

    protected override bool TryGetValueForAvatar(IRoomAvatar avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse((int)avatar.AvatarType);

        return true;
    }
}
