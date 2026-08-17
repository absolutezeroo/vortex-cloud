using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Grains.Systems;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

/// <summary>
/// A persistent high-score board (the twelve <c>highscore_*</c> furni, DB logic key
/// <c>wf_highscore</c> — the Arcturus name; the client logic is <c>furniture_high_score</c>, which
/// opens its widget on state 1 and renders the rows from the format-6 highscore stuff data).
/// <para>
/// What the board tracks is written in its classname, Arcturus-style:
/// <c>highscore_&lt;scoretype&gt;*&lt;variant&gt;</c> where scoretype is <c>perteam</c>(0) /
/// <c>mostwin</c>(1) / <c>classic</c>(2) and variant 1..4 maps to alltime/daily/weekly/monthly.
/// The round results arrive from <see cref="RoomGameScoreboardSystem"/> on GAME_ENDS; entries are
/// timestamped so the windowed variants prune themselves at rebuild time. Until per-player scoring
/// exists, classic records per team like perteam — the shared scores are per team.
/// </para>
/// </summary>
[RoomObjectLogic("wf_highscore")]
public sealed class FurnitureHighScoreLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    private const int StateClosed = 0;
    private const int StateOpen = 1;

    private const int ScoreTypeMostWin = 1;

    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Controller;

    public override async Task OnAttachAsync(CancellationToken ct)
    {
        if (StuffData is IHighscoreStuffData highscore)
        {
            // The board's identity comes from its classname; stamp it once so the client's widget
            // shows the right header, and rebuild so a reloaded daily/weekly board wakes up current.
            if (highscore.ScoreType < 0 || highscore.ClearType < 0)
            {
                (int scoreType, int clearType) = ParseClassname(_ctx.Definition.Name);
                highscore.SetScoreType(scoreType);
                highscore.SetClearType(clearType);
            }

            highscore.RebuildRows(DateTime.UtcNow.Ticks);

            await PersistStuffDataAsync(refresh: false);
        }

        await base.OnAttachAsync(ct);
    }

    /// <summary>Toggles the widget open/closed (the client opens it on state 1).</summary>
    public override Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct) =>
        SetStateAsync(GetState() == StateOpen ? StateClosed : StateOpen);

    /// <summary>Records a finished round per the board's score type and persists the board.</summary>
    public async Task RecordRoundAsync(GameRoundResult result, CancellationToken ct)
    {
        if (StuffData is not IHighscoreStuffData highscore)
        {
            return;
        }

        long now = DateTime.UtcNow.Ticks;

        if (highscore.ScoreType == ScoreTypeMostWin)
        {
            // Most-wins boards count victories: one winning entry per round.
            if (
                result.MemberNames.TryGetValue(
                    result.WinningTeam,
                    out IReadOnlyList<string>? winners
                )
                && winners.Count > 0
            )
            {
                highscore.RecordEntry(
                    new HighscoreEntry
                    {
                        Score = 1,
                        Win = true,
                        RecordedUtcTicks = now,
                        Names = [.. winners],
                    },
                    now
                );
            }
        }
        else
        {
            // Score boards record every team that scored, the winner flagged as such.
            foreach ((GameTeamColor team, int score) in result.Scores)
            {
                if (
                    score <= 0
                    || !result.MemberNames.TryGetValue(team, out IReadOnlyList<string>? members)
                    || members.Count == 0
                )
                {
                    continue;
                }

                highscore.RecordEntry(
                    new HighscoreEntry
                    {
                        Score = score,
                        Win = team == result.WinningTeam,
                        RecordedUtcTicks = now,
                        Names = [.. members],
                    },
                    now
                );
            }
        }

        await PersistStuffDataAsync(refresh: true);
    }

    private static (int ScoreType, int ClearType) ParseClassname(string classname)
    {
        int scoreType = 2; // classic

        if (classname.Contains("perteam"))
        {
            scoreType = 0;
        }
        else if (classname.Contains("mostwin"))
        {
            scoreType = 1;
        }

        // "*1".."*4" -> alltime(0) / daily(1) / weekly(2) / monthly(3); no variant reads alltime.
        int clearType = 0;
        int star = classname.LastIndexOf('*');

        if (
            star >= 0
            && int.TryParse(classname[(star + 1)..], out int variant)
            && variant is >= 1 and <= 4
        )
        {
            clearType = variant - 1;
        }

        return (scoreType, clearType);
    }
}
