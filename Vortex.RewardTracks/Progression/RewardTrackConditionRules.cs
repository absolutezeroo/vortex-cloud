using System.Collections.Generic;
using System.Globalization;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Admin;

namespace Vortex.RewardTracks.Progression;

/// <summary>
/// Which condition an operator is allowed to save.
/// </summary>
/// <remarks>
/// Pure and shared: the admin service refuses a bad condition at the form, and
/// <see cref="TaskProgressRules"/> refuses to act on one at runtime. Both halves are needed and
/// neither is redundant — the runtime protects a row that reached the table some other way (a
/// hand-written seed, a restored backup), and the form is what tells a person why.
/// <para>
/// The failure this exists to prevent is silent. A condition that cannot be satisfied does not
/// error; the task just never advances, and the operator has no way to tell that from "nobody has
/// done it yet".
/// </para>
/// </remarks>
public static class RewardTrackConditionRules
{
    /// <summary>
    /// The first thing wrong with a condition list, as an error code, or <c>null</c> when it is
    /// saveable. Returns one problem rather than all of them: the form shows one message.
    /// </summary>
    public static string? FirstProblem(IReadOnlyList<RewardTrackTaskConditionSpec>? conditions)
    {
        if (conditions is null)
        {
            return null;
        }

        foreach (RewardTrackTaskConditionSpec condition in conditions)
        {
            string value = condition.Value?.Trim() ?? string.Empty;

            if (value.Length == 0)
            {
                return "condition_value_required";
            }

            if (!IsOperatorAllowed(condition.Field, condition.Operator))
            {
                // The pairing that matters: AtLeast/AtMost on a target. The target is an opaque id
                // -- a room, an offer, a Habbicon -- and asking whether one id is "at least"
                // another answers a question nobody asked.
                return "condition_operator_not_valid_for_field";
            }

            if (RequiresNumber(condition.Field, condition.Operator) && !IsNumber(value))
            {
                return "condition_value_must_be_a_number";
            }

            if (condition.Operator == TaskConditionOperator.OneOf && !HasTwoOrMoreEntries(value))
            {
                // One entry in a list is Equals wearing a hat, and it reads as a mistake -- most
                // often a list typed with the wrong separator.
                return "condition_one_of_needs_a_list";
            }
        }

        return null;
    }

    /// <summary>Whether this operator means anything applied to this field.</summary>
    public static bool IsOperatorAllowed(TaskConditionField field, TaskConditionOperator op) =>
        field switch
        {
            TaskConditionField.Target => op
                is TaskConditionOperator.Equals
                    or TaskConditionOperator.NotEquals
                    or TaskConditionOperator.OneOf,
            TaskConditionField.Amount => op
                is TaskConditionOperator.Equals
                    or TaskConditionOperator.NotEquals
                    or TaskConditionOperator.AtLeast
                    or TaskConditionOperator.AtMost,
            _ => false,
        };

    private static bool RequiresNumber(TaskConditionField field, TaskConditionOperator op) =>
        field == TaskConditionField.Amount && op != TaskConditionOperator.OneOf;

    private static bool IsNumber(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

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
