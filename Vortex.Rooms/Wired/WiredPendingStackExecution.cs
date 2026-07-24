using System.Collections.Generic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Wired;

internal sealed class WiredPendingStackExecution
{
    public required IWiredStack Stack { get; init; }
    public required List<IWiredAction> Actions { get; init; }
    public required IWiredTrigger Trigger { get; init; }
    public required IWiredPolicy Policy { get; init; }
    public required IWiredSelectionSet Selected { get; init; }
    public required IWiredSelectionSet SelectorPool { get; init; }
    public required IWiredSelectionSet Signal { get; init; }

    /// <summary>The processing context the trigger fired with, kept so the stack's addon hooks
    /// (Before/AfterEffects) run against the real firing context when the chain actually executes —
    /// which can be ticks later than the scheduling.</summary>
    public required IWiredProcessingContext ProcessingContext { get; init; }

    public long Version { get; set; }
    public long DueAtMs { get; set; }
    public int NextActionIndex { get; set; }
    public int? WaitingActionIndex { get; set; }

    /// <summary>True once the chain has begun executing, so BeforeEffects hooks fire exactly once
    /// even when the chain suspends on a delayed action and resumes on a later tick.</summary>
    public bool EffectsStarted { get; set; }
}
