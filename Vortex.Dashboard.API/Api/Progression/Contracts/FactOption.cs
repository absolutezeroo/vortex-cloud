using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One fact an action emits, and how an editor may compare against it.
/// </summary>
/// <remarks>
/// Named FactOption rather than SignalFact because Vortex.Primitives owns that name for the
/// shape a translator declares; this is what the editor is offered.
/// </summary>
/// <param name="Operators">Which operators mean anything here. Sent rather than inferred, because
/// the validator refuses the others and an editor offering them would invite a save it knows fails.</param>
/// <param name="Values">The closed set, when the fact has one; empty otherwise.</param>
public sealed record FactOption(
    string Key,
    string Kind,
    string LabelKey,
    string FallbackLabel,
    IReadOnlyList<int> Operators,
    IReadOnlyList<FactOptionValue> Values
);
