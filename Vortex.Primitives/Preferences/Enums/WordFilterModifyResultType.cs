namespace Vortex.Primitives.Preferences.Enums;

/// <summary>
/// The result codes the client's word-filter dialog branches on.
/// </summary>
/// <remarks>
/// The AS3 event declares three constants — 0, 1 and 3 — but tests only two of them:
/// <see cref="Added"/> on the add path and <see cref="Removed"/> on the remove path. Nothing in the
/// client reads 0, so its meaning is not recoverable from that tree; it is named here for what the
/// server uses it as, and a client of that revision will simply ignore it. Note there is no 2.
/// </remarks>
public enum WordFilterModifyResultType
{
    /// <summary>Refused. The client ignores this, so the word silently stays as it was.</summary>
    Failed = 0,

    /// <summary>The word is now on the filter.</summary>
    Added = 1,

    /// <summary>The word is no longer on the filter.</summary>
    Removed = 3,
}
