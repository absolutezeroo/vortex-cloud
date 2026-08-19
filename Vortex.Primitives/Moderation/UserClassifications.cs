namespace Vortex.Primitives.Moderation;

/// <summary>
/// The classification keys the client can ask for. Only the two the WIN63 source actually names are
/// here: <c>UserClassificationData</c> declares four constants but obfuscation stripped the names of
/// 1 and 3, and inventing a meaning for them would be inventing protocol.
/// </summary>
public static class UserClassifications
{
    /// <summary>Sent by the <c>:anew</c> command verbatim; matches
    /// <c>UserClassificationData.NEW_USER_CLASSIFICATION</c>.</summary>
    public const string New = "new";

    /// <summary>Matches <c>UserClassificationData.PAYING_USER_CLASSIFICATION</c>.</summary>
    public const string Paying = "paying";

    /// <summary>The hotel-wide scope keyword: <c>:uc hotel &lt;classification&gt;</c> classifies
    /// everyone online instead of everyone in the room.</summary>
    public const string HotelScopeKeyword = "hotel";
}
