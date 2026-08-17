using System.Collections.Generic;

namespace Vortex.Primitives.Furniture.StuffData;

public interface IHighscoreStuffData : IStuffData
{
    public int ScoreType { get; }
    public int ClearType { get; }
    public Dictionary<int, List<string>> HighscoreData { get; set; }

    /// <summary>The recorded rounds this board's display is built from. The display rows in
    /// <see cref="HighscoreData"/> are a projection of these through the board's score type and
    /// clear-type window — never write the rows directly.</summary>
    public List<HighscoreEntry> Entries { get; set; }

    public void SetScoreType(int scoreType);

    public void SetClearType(int clearType);

    /// <summary>Appends a round and rebuilds the display rows for the current window.</summary>
    public void RecordEntry(HighscoreEntry entry, long nowUtcTicks);

    /// <summary>Prunes entries that fell out of the clear-type window and rebuilds the display rows
    /// from what remains — called at attach so a reloaded daily/weekly board wakes up current.</summary>
    public void RebuildRows(long nowUtcTicks);
}
