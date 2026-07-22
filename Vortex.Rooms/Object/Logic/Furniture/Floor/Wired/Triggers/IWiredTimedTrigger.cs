namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// A wired trigger driven by the room clock rather than a queued room event — the periodic triggers
/// and the one-shot "trigger once" trigger. The wired system polls these every tick instead of
/// routing an event to them.
/// </summary>
public interface IWiredTimedTrigger
{
    /// <summary>
    /// Returns <c>true</c> if the trigger is due to fire at <paramref name="nowMs"/>, advancing
    /// (periodic) or disarming (one-shot) its internal schedule as it does so; <c>false</c> otherwise.
    /// </summary>
    bool TryConsumeDue(long nowMs);
}
