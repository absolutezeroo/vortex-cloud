namespace Vortex.Primitives.Rooms.Wired.Variable;

/// <summary>
/// A variable that owns no value and reads one out of another variable — a level-up add-on's
/// level, progress and XP-remaining readings of an experience variable.
/// </summary>
/// <remarks>
/// The room needs this to answer one question the reading cannot answer for itself: the write
/// always lands on the source, so the source is the only thing that ever announces a change. A
/// reading is a variable in its own right in every picker the client shows — it has its own id and
/// the "variable changed" trigger offers it — so the room turns the source's change into one change
/// per reading, and that needs the source's id and the arithmetic between the two.
/// </remarks>
public interface IWiredDerivedVariable
{
    /// <summary>The variable this one reads.</summary>
    WiredVariableId SourceVariableId { get; }

    /// <summary>What this reading answers when the source holds <paramref name="sourceValue"/>.
    /// </summary>
    int ValueFor(int sourceValue);
}
