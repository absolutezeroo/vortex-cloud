using System.Threading.Tasks;

namespace Vortex.Plugins.Exports;

/// <summary>
/// Non-generic view of a <see cref="ReloadableExport{T}" /> slot, so <see cref="ExportRegistry" />
/// and <see cref="ExportJournal" /> — which only ever hold <c>object</c> — can undo a bind without
/// knowing the closed generic type.
/// </summary>
internal interface IExportSlot
{
    /// <summary>
    /// Publishes <paramref name="value" /> and returns whatever was published before it,
    /// <em>without disposing it</em>. The caller owns the returned instance: it must either dispose
    /// it (the activation committed) or hand it back to <see cref="RollbackAsync" /> (the activation
    /// failed).
    /// </summary>
    Task<object?> SwapDeferredAsync(object value);

    /// <summary>
    /// Restores <paramref name="previous" /> as the published instance and disposes
    /// <paramref name="failed" />, the value that a failed activation had published.
    /// </summary>
    Task RollbackAsync(object? previous, object failed);

    /// <summary>Disposes a superseded instance once the activation that replaced it has committed.</summary>
    Task DisposeSupersededAsync(object? instance);
}
