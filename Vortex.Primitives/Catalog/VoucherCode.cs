namespace Vortex.Primitives.Catalog;

/// <summary>
/// What a voucher code may look like, decided once so the answer is the same everywhere.
/// </summary>
/// <remarks>
/// <para>
/// This exists because of the grain key. <c>GetVoucherGrain(code)</c> names an Orleans grain after
/// the string the client sent, so every distinct string tried is an activation, and every
/// activation is a database read in <c>OnActivateAsync</c>. Without a bound, a client chose how
/// much memory and how many queries one packet cost (SEC-11).
/// </para>
/// <para>
/// The bound is not a guess: <c>VoucherEntity.Code</c> is <c>[MaxLength(64)]</c>, so a longer
/// string cannot match a stored row under any circumstance. Refusing it costs nothing that could
/// ever have succeeded, which is what makes this safe to apply to codes already in circulation.
/// Restricting which CHARACTERS are allowed would not be safe the same way -- operators author
/// these codes and the existing ones cannot be enumerated from here -- so this does not.
/// </para>
/// </remarks>
public static class VoucherCode
{
    /// <summary>Mirrors <c>VoucherEntity.Code</c>'s column length. Longer cannot match a row.</summary>
    public const int MaxLength = 64;

    /// <summary>
    /// Whether this string could name a real voucher. Rejects only what cannot be one: empty, over
    /// the column length, or carrying a control character no client could type.
    /// </summary>
    public static bool IsWellFormed(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > MaxLength)
        {
            return false;
        }

        foreach (char c in code)
        {
            if (char.IsControl(c))
            {
                return false;
            }
        }

        return true;
    }
}
