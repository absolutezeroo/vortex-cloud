using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Vortex.Plugins.Exports;

/// <summary>
/// Records every export a single plugin activation publishes, together with the instance it
/// displaced, so the activation can be undone.
/// <para>
/// Without this there is no way back: <see cref="ExportRegistry" /> never removes entries, the slot
/// keeps only the current value, and the previous instance used to be disposed the moment it was
/// replaced. A plugin that failed halfway through starting therefore left its (possibly
/// non-functional) objects published globally, with the working ones already destroyed.
/// </para>
/// </summary>
internal sealed class ExportJournal(ILogger logger)
{
    private readonly List<Entry> _entries = [];

    /// <summary>Number of export binds recorded so far — used in rollback log lines.</summary>
    public int Count => _entries.Count;

    public void Record(IExportSlot slot, string exportKey, object? previous, object published) =>
        _entries.Add(new Entry(slot, exportKey, previous, published));

    /// <summary>
    /// The activation succeeded: the displaced instances are genuinely dead now, so release them.
    /// </summary>
    public async Task CommitAsync()
    {
        foreach (Entry entry in _entries)
        {
            await entry.Slot.DisposeSupersededAsync(entry.Previous).ConfigureAwait(false);
        }

        _entries.Clear();
    }

    /// <summary>
    /// The activation failed: restore each displaced instance and dispose what the failed activation
    /// published, in reverse bind order.
    /// </summary>
    public async Task RollbackAsync(string pluginKey)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry entry = _entries[i];

            try
            {
                await entry
                    .Slot.RollbackAsync(entry.Previous, entry.Published)
                    .ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                logger.LogError(
                    ex,
                    "Rollback step failed: could not restore export '{ExportKey}' for plugin "
                        + "{PluginKey}. The export may still point at the failed activation's "
                        + "instance.",
                    entry.ExportKey,
                    pluginKey
                );
            }
        }

        _entries.Clear();
    }

    private sealed record Entry(
        IExportSlot Slot,
        string ExportKey,
        object? Previous,
        object Published
    );
}
