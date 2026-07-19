namespace Vortex.Primitives.Observability;

/// <summary>
/// Durable, append-only history of an item's lifecycle, indexed by item id so the full story of any
/// furniture id can be reconstructed (creation, ownership changes, placement, pickup, deletion, ...).
/// Implementations are non-blocking.
/// </summary>
public interface IItemForensics
{
    void Record(in ItemForensicEvent itemEvent);
}
