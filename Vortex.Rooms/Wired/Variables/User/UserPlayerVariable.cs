using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// A reading only a real player carries — an account id, a guild, a sanction. A pet or a bot standing
/// on the same tile simply does not resolve it, which is the same silence the base already keeps for
/// an avatar that has left.
/// </summary>
public abstract class UserPlayerVariable(RoomGrain roomGrain)
    : UserVariable<IRoomPlayer>(roomGrain);
