using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

public sealed class UserTypeVariable(RoomGrain roomGrain) : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override string VariableName => "@type";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 10;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.CanWriteValue
        | WiredVariableFlags.HasTextConnector;

    protected override Dictionary<WiredVariableValue, string> GetTextConnectors() =>
        Enum.GetValues<RoomObjectType>()
            .ToDictionary(
                v => WiredVariableValue.Parse((int)v),
                v => RoomObjectTypeExtensions.GetString(v)
            );

    protected override WiredVariableValue GetValueForAvatar(IRoomAvatar avatar) =>
        WiredVariableValue.Parse((int)avatar.AvatarType);
}
