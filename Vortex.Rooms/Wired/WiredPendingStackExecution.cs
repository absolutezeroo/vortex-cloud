using System.Collections.Generic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Grains.Storage;

namespace Vortex.Rooms.Wired;

internal sealed class WiredPendingStackExecution
{
    public required IWiredStack Stack { get; init; }
    public required List<IWiredAction> Actions { get; init; }
    public IWiredTrigger? Trigger { get; init; }
    public required IWiredPolicy Policy { get; init; }
    public required IWiredSelectionSet Selected { get; init; }
    public required IWiredSelectionSet SelectorPool { get; init; }
    public required IWiredSelectionSet Signal { get; init; }

    /// <summary>The processing context the trigger fired with, kept so the stack's addon hooks
    /// (Before/AfterEffects) run against the real firing context when the chain actually executes —
    /// which can be ticks later than the scheduling.</summary>
    public required IWiredProcessingContext ProcessingContext { get; init; }

    /// <summary>
    /// The values held by this firing's context variables (<c>wf_var_context</c>).
    /// </summary>
    /// <remarks>
    /// On the pending chain rather than on the execution context, because a fresh
    /// <c>WiredExecutionContext</c> is built for every action in the chain — a store living there
    /// would be thrown away between one action and the next, which is the opposite of what a
    /// variable written by one effect and read by the one after it is for. The chain is the scope
    /// the client's own name implies: a context variable describes this run and does not outlive it,
    /// and that includes a chain that suspends on a delayed action and resumes ticks later.
    /// </remarks>
    public KeyValueStore ContextVariables { get; } = new();

    public long Version { get; set; }
    public long DueAtMs { get; set; }
    public int NextActionIndex { get; set; }
    public int? WaitingActionIndex { get; set; }

    /// <summary>True once the chain has begun executing, so BeforeEffects hooks fire exactly once
    /// even when the chain suspends on a delayed action and resumes on a later tick.</summary>
    public bool EffectsStarted { get; set; }
}
