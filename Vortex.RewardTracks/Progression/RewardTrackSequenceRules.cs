using System;
using System.Collections.Generic;
using System.Globalization;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Admin;

namespace Vortex.RewardTracks.Progression;

/// <summary>
/// Which sequence an operator is allowed to save.
/// </summary>
/// <remarks>
/// <para>
/// Everything refused here fails silently if it is allowed through: a task whose step can never be
/// satisfied does not error, it just stops advancing, and nobody can tell that apart from "no one
/// has done it yet". So the editor refuses it while the operator is still looking at the form.
/// </para>
/// <para>
/// The rule that earns its keep is the fact check. <c>place_item</c> emits no <c>player</c>, so a
/// step filtering it on one is a dead task — and there is nothing on any screen that would say so.
/// <see cref="RewardTrackActionFacts"/> is the list, and it is also what the dashboard offers, so
/// the two cannot disagree.
/// </para>
/// </remarks>
public static class RewardTrackSequenceRules
{
    /// <summary>
    /// The first thing wrong with a sequence, as an error code, or <c>null</c> when it is saveable.
    /// One problem rather than all of them: the form shows one message.
    /// </summary>
    public static string? FirstProblem(IReadOnlyList<RewardTrackTaskStepSpec>? steps)
    {
        if (steps is null || steps.Count == 0)
        {
            // A task with no steps is legal on the way in: the catalog builds one from the task's
            // own action. Only an explicitly empty list is a mistake worth naming.
            return steps is null ? null : "sequence_needs_a_step";
        }

        for (int index = 0; index < steps.Count; index++)
        {
            RewardTrackTaskStepSpec step = steps[index];

            if (string.IsNullOrWhiteSpace(step.ActionCode))
            {
                return "step_action_required";
            }

            foreach (RewardTrackStepFilterSpec filter in step.Filters ?? [])
            {
                if (Problem(steps, index, step, filter) is string problem)
                {
                    return problem;
                }
            }
        }

        return null;
    }

    private static string? Problem(
        IReadOnlyList<RewardTrackTaskStepSpec> steps,
        int index,
        RewardTrackTaskStepSpec step,
        RewardTrackStepFilterSpec filter
    )
    {
        string value = filter.Value?.Trim() ?? string.Empty;

        if (value.Length == 0 || string.IsNullOrWhiteSpace(filter.FactKey))
        {
            return "filter_incomplete";
        }

        if (!RewardTrackActionFacts.Emits(step.ActionCode, filter.FactKey))
        {
            // The step's own action never reports this. Nothing would ever satisfy the filter.
            return "filter_fact_not_emitted_by_action";
        }

        int referenced = BackReference(value);

        if (referenced < 0)
        {
            return filter.Operator == StepFilterOperator.OneOf && !HasTwoOrMoreEntries(value)
                ? "filter_one_of_needs_a_list"
                : null;
        }

        if (referenced >= index)
        {
            // Pointing at itself or forwards. The step it names has not run when this one is tested.
            return "filter_reference_must_be_earlier";
        }

        return RewardTrackActionFacts.Emits(steps[referenced].ActionCode, filter.FactKey)
            ? null
            // The earlier step never recorded this fact, so there is nothing for $N to resolve to.
            // "The same furniture" only works between two steps that both talk about furniture.
            : "filter_reference_fact_not_captured";
    }

    /// <summary>The step a <c>$N</c> value points at, or <c>-1</c> when the value is a literal.</summary>
    public static int BackReference(string value) =>
        value.Length > 1
        && value[0] == '$'
        && int.TryParse(
            value.AsSpan(1),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int step
        )
            ? step
            : -1;

    private static bool HasTwoOrMoreEntries(string value)
    {
        int entries = 0;

        foreach (string part in value.Split(','))
        {
            if (part.Trim().Length > 0)
            {
                entries++;
            }
        }

        return entries >= 2;
    }
}
