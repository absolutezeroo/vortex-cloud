using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Keeps every scoreboard furni painted from the room's shared scores, driven by the same events the
/// wired boxes read — no game refreshes a board by hand any more. A score change through
/// <see cref="RoomGameSystem.AddTeamScoreAsync"/> or a wired give-score box repaints the matching
/// colour's boards; GAME_STARTS zeroes them (the coordinator has already reset the shared scores);
/// GAME_ENDS pushes the final tally and hands the round's result to every high-score board in the
/// room. That last part means a give-score box now also paints boards outside a game round — which
/// is what the boards do on Habbo.
/// </summary>
public sealed class RoomGameScoreboardSystem(RoomGrain roomGrain) : IRoomEventListener
{
    private readonly RoomGrain _roomGrain = roomGrain;

    public async Task OnRoomEventAsync(RoomEvent evt, CancellationToken ct)
    {
        switch (evt)
        {
            case WiredTeamScoreChangedEvent scoreChanged:
                await PaintBoardsAsync(scoreChanged.Team, scoreChanged.Score);
                break;
            case WiredGameStartedEvent:
                await PaintAllBoardsAsync();
                break;
            case WiredGameEndedEvent:
                await PaintAllBoardsAsync();
                await RecordRoundOnHighScoreBoardsAsync(ct);
                break;
        }
    }

    private async Task PaintBoardsAsync(GameTeamColor team, int score)
    {
        foreach (
            FurnitureScoreboardLogic board in _roomGrain._state.ItemIndex.LogicsOf<FurnitureScoreboardLogic>()
        )
        {
            if (board.TeamColor == team)
            {
                await board.SetStateAsync(score);
            }
        }
    }

    /// <summary>Repaints every board from the shared scores — what a round start (all zeros) and a
    /// round end (the final tally) both need.</summary>
    private async Task PaintAllBoardsAsync()
    {
        foreach (
            FurnitureScoreboardLogic board in _roomGrain._state.ItemIndex.LogicsOf<FurnitureScoreboardLogic>()
        )
        {
            if (GameTeamState.IsRealTeam(board.TeamColor))
            {
                await board.SetStateAsync(_roomGrain.GameSystem.GetTeamScore(board.TeamColor));
            }
        }
    }

    /// <summary>Builds the round's result from the still-standing shared state and offers it to every
    /// high-score board. Runs on GAME_ENDS, before players start leaving their teams.</summary>
    private async Task RecordRoundOnHighScoreBoardsAsync(CancellationToken ct)
    {
        List<FurnitureHighScoreLogic> boards =
            _roomGrain._state.ItemIndex.LogicsOf<FurnitureHighScoreLogic>();

        if (boards.Count == 0)
        {
            return;
        }

        GameRoundResult result = BuildRoundResult();

        // A scoreless round records nothing — an empty board after ten empty rounds should still
        // be empty, and Arcturus only records rounds somebody scored in.
        if (result.WinningTeam == GameTeamColor.None)
        {
            return;
        }

        foreach (FurnitureHighScoreLogic board in boards)
        {
            await board.RecordRoundAsync(result, ct);
        }
    }

    private GameRoundResult BuildRoundResult()
    {
        Dictionary<GameTeamColor, int> scores = [];
        Dictionary<GameTeamColor, IReadOnlyList<string>> names = [];

        for (
            GameTeamColor team = GameTeamColor.Red;
            team <= GameTeamColor.Yellow;
            team = (GameTeamColor)((int)team + 1)
        )
        {
            scores[team] = _roomGrain.GameSystem.GetTeamScore(team);

            List<string> members = [];

            foreach (PlayerId playerId in _roomGrain.GameSystem.GetPlayersInTeam(team))
            {
                if (
                    _roomGrain._state.AvatarsByPlayerId.TryGetValue(
                        playerId,
                        out RoomObjectId objectId
                    )
                    && _roomGrain._state.AvatarsByObjectId.TryGetValue(
                        objectId,
                        out IRoomAvatar? avatar
                    )
                )
                {
                    members.Add(avatar.Name);
                }
            }

            names[team] = members;
        }

        return new GameRoundResult
        {
            WinningTeam = _roomGrain.GameSystem.TeamState.GetLeadingTeam(),
            Scores = scores,
            MemberNames = names,
        };
    }
}
