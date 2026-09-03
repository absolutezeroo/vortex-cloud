using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Rooms.Games.Events;
using Vortex.Rooms.Grains;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.Presentation;

/// <summary>
/// Paints every scoreboard in the room from the shared scores, and hands a finished round to the
/// high-score boards. This is the presentation adapter for scoring: no game repaints a board, and no
/// board knows which game produced the number on it.
/// <para>
/// It listens on the ROOM's events rather than a single game's, because that is what a scoreboard
/// is: a wired give-score box outside any match paints the boards too, which is what they do on
/// Habbo, and a room whose arena failed to validate still zeroes its boards when the timer is
/// pressed.
/// </para>
/// <para>
/// Complexity is O(boards of the changed colour) per score and O(boards) per round boundary — the
/// item index answers "the scoreboards in this room" directly, so neither is a room scan.
/// </para>
/// </summary>
public sealed class GameScoreboardPresenter(RoomGrain roomGrain) : IRoomEventListener
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
            IScoreDisplayComponent board in _roomGrain._state.ItemIndex.LogicsOf<IScoreDisplayComponent>()
        )
        {
            if (board.Team == team)
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
            IScoreDisplayComponent board in _roomGrain._state.ItemIndex.LogicsOf<IScoreDisplayComponent>()
        )
        {
            if (HabboTeamPalette.IsColour(board.Team))
            {
                await board.SetStateAsync(_roomGrain.GameRuntime.GetTeamScore(board.Team));
            }
        }
    }

    /// <summary>Builds the round's result from the still-standing shared state and offers it to
    /// every high-score board. Runs on GAME_ENDS, before players start leaving their teams.</summary>
    private async Task RecordRoundOnHighScoreBoardsAsync(CancellationToken ct)
    {
        List<FurnitureHighScoreLogic> boards =
            _roomGrain._state.ItemIndex.LogicsOf<FurnitureHighScoreLogic>();

        if (boards.Count == 0)
        {
            return;
        }

        GameMatchResult result = BuildRoundResult();

        // A scoreless round records nothing — an empty board after ten empty rounds should still be
        // empty.
        if (result.WinningTeam == GameTeamColor.None)
        {
            return;
        }

        foreach (FurnitureHighScoreLogic board in boards)
        {
            await board.RecordRoundAsync(result, ct);
        }
    }

    /// <summary>
    /// Projects the room's ledger onto the four Habbo colours for the boards, which is the only shape
    /// a <c>highscore_*</c> furni can record. This walk over the four IS a Habbo enumeration and
    /// belongs here: it is asking "what can the boards show", not "what teams exist".
    /// </summary>
    private GameMatchResult BuildRoundResult()
    {
        Dictionary<GameTeamColor, int> scores = [];
        Dictionary<GameTeamColor, IReadOnlyList<string>> names = [];

        foreach (GameTeamColor team in HabboTeamPalette.Colours)
        {
            scores[team] = _roomGrain.GameRuntime.GetTeamScore(team);

            List<string> members = [];

            foreach (PlayerId playerId in _roomGrain.GameRuntime.GetPlayersInTeam(team))
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

        return new GameMatchResult
        {
            WinningTeam = _roomGrain.GameRuntime.LeadingTeam,
            Scores = scores,
            MemberNames = names,
        };
    }
}
