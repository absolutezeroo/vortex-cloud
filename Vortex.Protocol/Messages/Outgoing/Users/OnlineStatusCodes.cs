namespace Vortex.Protocol.Messages.Outgoing.Users;

/// <summary>
/// The three values <c>ExtendedProfileMessageComposer.OnlineStatus</c> can carry.
///
/// Named after the client's own constants, which are readable even though the class holding them
/// is obfuscated: WIN63 <c>unknowns/_SafePkg_1731/_SafeCls_2228.as</c> declares them as three
/// <c>public static const int</c> on the extended-profile parser, and the profile window switches
/// its status icon on the value.
/// </summary>
public static class OnlineStatusCodes
{
    /// <summary>Not connected.</summary>
    public const int Offline = 0;

    /// <summary>Connected and visible.</summary>
    public const int Online = 1;

    /// <summary>Connected but hiding their presence — the state a bool could never express, and
    /// the reason this field is a byte.</summary>
    public const int Hidden = 2;
}
