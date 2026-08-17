using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Furniture.StuffData;

namespace Vortex.Furniture.StuffData;

internal sealed class HighscoreStuffData : StuffDataBase, IHighscoreStuffData
{
    /// <summary>The most rows a board displays — Arcturus caps its boards at 50 too.</summary>
    private const int MaxDisplayRows = 50;

    // Arcturus WiredHighscoreScoreType value, matched by the client's HighScoreData display —
    // per-team (0) and classic (2) both build plain score rows; most-wins builds win counts.
    private const int ScoreTypeMostWin = 1;

    [JsonIgnore]
    public override StuffDataType StuffType => StuffDataType.HighscoreKey;

    public string Data { get; set; } = DEFAULT_STATE;
    public int ScoreType { get; set; } = -1;
    public int ClearType { get; set; } = -1;
    public Dictionary<int, List<string>> HighscoreData { get; set; } = [];
    public List<HighscoreEntry> Entries { get; set; } = [];

    public override string GetLegacyString() => Data;

    public override void SetState(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            state = "0";
        }

        Data = state;

        MarkDirty();
    }

    public int GetScoreType() => ScoreType;

    public void SetScoreType(int scoreType)
    {
        ScoreType = scoreType;

        MarkDirty();
    }

    public int GetClearType() => ClearType;

    public void SetClearType(int clearType)
    {
        ClearType = clearType;

        MarkDirty();
    }

    public void SetScore(int score, string name)
    {
        if (!HighscoreData.TryGetValue(score, out List<string>? value))
        {
            value = [name];
            HighscoreData[score] = value;

            MarkDirty();

            return;
        }

        if (!value.Contains(name))
        {
            value.Add(name);

            MarkDirty();
        }
    }

    public void RecordEntry(HighscoreEntry entry, long nowUtcTicks)
    {
        Entries.Add(entry);

        RebuildRows(nowUtcTicks);
    }

    public void RebuildRows(long nowUtcTicks)
    {
        // Entries that fell out of a daily/weekly/monthly window are gone for good — that is what
        // the board's clear type means. Alltime (or an unset clear type) keeps everything.
        long windowTicks = ClearType switch
        {
            1 => TimeSpan.FromDays(1).Ticks,
            2 => TimeSpan.FromDays(7).Ticks,
            3 => TimeSpan.FromDays(30).Ticks,
            _ => 0,
        };

        if (windowTicks > 0)
        {
            Entries.RemoveAll(entry => nowUtcTicks - entry.RecordedUtcTicks > windowTicks);
        }

        HighscoreData = ScoreType == ScoreTypeMostWin ? BuildMostWinRows() : BuildScoreRows();

        MarkDirty();
    }

    /// <summary>Classic / per-team rows: the top scores, each with the names that achieved them.</summary>
    private Dictionary<int, List<string>> BuildScoreRows()
    {
        Dictionary<int, List<string>> rows = [];

        foreach (
            HighscoreEntry entry in Entries
                .OrderByDescending(entry => entry.Score)
                .Take(MaxDisplayRows)
        )
        {
            if (!rows.TryGetValue(entry.Score, out List<string>? names))
            {
                names = [];
                rows[entry.Score] = names;
            }

            foreach (string name in entry.Names)
            {
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }
        }

        return rows;
    }

    /// <summary>Most-wins rows: how many recorded wins each distinct set of names has.</summary>
    private Dictionary<int, List<string>> BuildMostWinRows()
    {
        Dictionary<string, (int Wins, List<string> Names)> bySet = [];

        foreach (HighscoreEntry entry in Entries.Where(entry => entry.Win))
        {
            string key = string.Join("\n", entry.Names);

            bySet[key] = bySet.TryGetValue(key, out (int Wins, List<string> Names) existing)
                ? (existing.Wins + 1, existing.Names)
                : (1, entry.Names);
        }

        Dictionary<int, List<string>> rows = [];

        foreach (
            (int wins, List<string> names) in bySet
                .Values.OrderByDescending(set => set.Wins)
                .Take(MaxDisplayRows)
        )
        {
            if (!rows.TryGetValue(wins, out List<string>? row))
            {
                row = [];
                rows[wins] = row;
            }

            foreach (string name in names)
            {
                if (!row.Contains(name))
                {
                    row.Add(name);
                }
            }
        }

        return rows;
    }

    protected override StuffDataSnapshot BuildSnapshot() =>
        new HighscoreStuffSnapshot()
        {
            StuffBitmask = GetBitmask(),
            UniqueNumber = UniqueNumber,
            UniqueSeries = UniqueSeries,
            Data = GetLegacyString(),
            ScoreType = GetScoreType(),
            ClearType = GetClearType(),
            Scores = HighscoreData.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableArray()
            ),
        };
}
