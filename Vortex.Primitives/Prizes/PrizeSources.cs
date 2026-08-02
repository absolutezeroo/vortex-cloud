namespace Vortex.Primitives.Prizes;

/// <summary>
/// What triggered a draw, recorded on the payout audit row. The pool says what could be won; this
/// says which furniture handed it over, which is what separates "the box pool is too generous" from
/// "one furniture is handing the box pool out for free".
/// </summary>
public static class PrizeSources
{
    public const string MysteryBox = "mystery-box";
}
