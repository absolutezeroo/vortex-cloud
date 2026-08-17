using System.Collections.Generic;

namespace Vortex.Primitives.Furniture.StuffData;

/// <summary>
/// One recorded round on a high-score board: the score, the names it belongs to, whether it was the
/// winning entry, and when it was recorded — the timestamp is what lets the daily/weekly/monthly
/// board variants prune their window at rebuild time instead of needing a scheduled wipe. Stored in
/// the item's persisted STUFF section (plain get/set properties for the JSON round trip).
/// </summary>
public sealed class HighscoreEntry
{
    public int Score { get; set; }

    public bool Win { get; set; }

    public long RecordedUtcTicks { get; set; }

    public List<string> Names { get; set; } = [];
}
