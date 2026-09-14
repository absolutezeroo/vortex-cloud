using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Copies what the event that started a chain says into the context, under the names the
/// <c>@event.…</c> variables read.
/// </summary>
/// <remarks>
/// One place rather than one per trigger: the pending execution keeps the firing
/// <see cref="IWiredProcessingContext"/>, which keeps the <see cref="RoomEvent"/> itself, so the
/// whole family can be filled where the execution context is built. A trigger that learns to carry
/// more only has to say so on its event.
/// <para>
/// The keys are the variable names, so there is one spelling of each reading in the codebase and no
/// table to keep in step.
/// </para>
/// </remarks>
internal static class WiredEventReadings
{
    public const string VariableOldValue = "@event.variable_update.old_value";
    public const string VariableNewValue = "@event.variable_update.new_value";
    public const string VariableDifference = "@event.variable_update.difference";
    public const string VariableChangeType = "@event.variable_update.change_type";

    /// <summary>
    /// Fills <paramref name="context"/> from <paramref name="evt"/>, for the readings that event
    /// actually carries.
    /// </summary>
    /// <remarks>
    /// Only the variable-change family is filled today, and not all of it. The official client also
    /// offers <c>variable_update.box_id</c> and <c>.change_origin</c>, the whole of
    /// <c>signal.antenna_id</c>, <c>chat.type</c>/<c>.style</c>, and both transaction families —
    /// and none of those values exist anywhere on this side yet: <c>SignalRoomEvent</c> carries no
    /// antenna, <c>PlayerChatEvent</c> carries only its message, and
    /// <c>WiredTransactionCompletedEvent</c> and <c>WiredTransactionFailedEvent</c> are empty
    /// records. Adding a reading means first making its event say the thing, at the place that
    /// raises it. Writing a zero here instead would be worse than the variable being absent: a
    /// builder cannot tell a real zero from a missing one.
    /// </remarks>
    public static void Populate(IWiredContext context, RoomEvent? evt)
    {
        if (evt is WiredVariableChangedEvent changed)
        {
            context.Variables[VariableOldValue] = changed.Previous;
            context.Variables[VariableNewValue] = changed.Current;
            context.Variables[VariableDifference] = changed.Current - changed.Previous;
            context.Variables[VariableChangeType] = (int)changed.Kind;
        }
    }
}
