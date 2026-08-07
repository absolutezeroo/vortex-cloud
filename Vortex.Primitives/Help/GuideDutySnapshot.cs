using Orleans;

namespace Vortex.Primitives.Help;

/// <summary>
/// A guide's own duty state, plus how many of each role are currently covering the queues.
/// </summary>
/// <remarks>
/// The three roles are separate queues, not seniority: a guide handles tour requests, a helper
/// handles help requests, and a guardian handles chat reviews. One person can cover any combination,
/// which is why the counts overlap and do not sum to the number of people on duty.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record GuideDutySnapshot
{
    [Id(0)]
    public required bool OnDuty { get; init; }

    [Id(1)]
    public required int GuidesOnDuty { get; init; }

    [Id(2)]
    public required int HelpersOnDuty { get; init; }

    [Id(3)]
    public required int GuardiansOnDuty { get; init; }
}
