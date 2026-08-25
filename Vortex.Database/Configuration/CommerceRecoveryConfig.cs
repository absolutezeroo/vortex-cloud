namespace Vortex.Database.Configuration;

/// <summary>
/// How often the commerce relay sweeps, and how long an operation may sit past its pivot before an
/// operator is told about it.
/// </summary>
/// <remarks>
/// These are budgets, so they are configured rather than hardcoded — the same rule the wired engine's
/// depth limit is held to. A hotel with a slow database wants a longer stuck threshold than a dev
/// server; neither wants to edit a constant to say so.
/// </remarks>
public sealed class CommerceRecoveryConfig
{
    public const string SECTION_NAME = "Vortex:Commerce:Recovery";

    /// <summary>Seconds between sweeps. The relay normally finds nothing: a flow publishes its own
    /// event immediately, and this is the crash path.</summary>
    public int SweepIntervalSeconds { get; init; } = 30;

    /// <summary>How many operations one sweep looks at, so a backlog cannot monopolise a tick.</summary>
    public int RelayBatchSize { get; init; } = 100;

    /// <summary>
    /// Minutes past its pivot after which an operation is escalated to
    /// <c>NeedsIntervention</c> and logged at critical. Long enough that an ordinary retry has had
    /// its chance; short enough that a player is not waiting a day for someone to notice.
    /// </summary>
    public int StuckAfterMinutes { get; init; } = 10;
}
