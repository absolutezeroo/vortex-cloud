namespace Vortex.Catalog.Grains;

/// <summary>
/// The persisted state machine for a single LTD raffle entry
/// (<c>catalog_ltd_raffle_entries.result</c>, a <c>varchar(20)</c>).
/// <para>
/// The batch's own state is derived from its entries rather than stored separately, so no schema
/// change was needed: a batch is <em>pending</em> while any entry is <see cref="PENDING" />,
/// <em>processing</em> while any is <see cref="PROCESSING" />, <em>retryable</em> while any is
/// <see cref="REFUND_FAILED" />, and <em>completed</em> once every entry is terminal. Every value
/// below fits the existing column width, and the two legacy values are still recognised so rows
/// written before this state machine existed stay readable.
/// </para>
/// </summary>
internal static class LtdRaffleEntryResults
{
    /// <summary>Entry paid for and recorded; the draw has not run yet.</summary>
    public const string PENDING = "pending";

    /// <summary>
    /// The draw has claimed this entry. Reaching this state means the outcome is decided but the
    /// external effect (inventory grant / wallet refund) may or may not have been applied — it is
    /// deliberately never auto-retried, because a retry could double-pay.
    /// </summary>
    public const string PROCESSING = "processing";

    /// <summary>Winner. The serial number is reserved and the series stock is already decremented.</summary>
    public const string WON = "won";

    /// <summary>Loser whose credits were returned.</summary>
    public const string REFUNDED = "refunded";

    /// <summary>
    /// Loser whose refund threw. The wallet call failed, so the credits were definitely not
    /// returned — safe to retry, and the only state the recovery pass acts on.
    /// </summary>
    public const string REFUND_FAILED = "refund_failed";

    /// <summary>
    /// Winner whose inventory grant threw after the serial was reserved. Never re-granted
    /// automatically: the serial is burnt and re-running would risk issuing the item twice.
    /// </summary>
    public const string GRANT_FAILED = "grant_failed";

    /// <summary>Legacy terminal value written before refunds were tracked per entry.</summary>
    public const string LEGACY_LOST = "lost";

    /// <summary>Legacy terminal value written when a batch was voided for lack of stock.</summary>
    public const string LEGACY_NO_REMAINING = "no_remaining";

    /// <summary>States the draw still has work to do for.</summary>
    public static bool IsOpen(string result) =>
        result is PENDING or PROCESSING or REFUND_FAILED or GRANT_FAILED;
}
