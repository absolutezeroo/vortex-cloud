using Vortex.Primitives.Messages.Outgoing.Game.Lobby;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Game.Arena;
using Vortex.Revisions.Revision20260701.Parsers.Game.Directory;
using Vortex.Revisions.Revision20260701.Parsers.Game.Ingame;
using Vortex.Revisions.Revision20260701.Parsers.Game.Lobby;
using Vortex.Revisions.Revision20260701.Parsers.Game.Score;
using Vortex.Revisions.Revision20260701.Serializers.Game.Lobby;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class GameMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        // Game Arena
        builder.MapParser(MessageEvent.Game2ExitGameMessageEvent, new Game2ExitGameMessageParser());
        builder.MapParser(MessageEvent.Game2GameChatMessageEvent, new Game2GameChatMessageParser());
        builder.MapParser(
            MessageEvent.Game2LoadStageReadyMessageEvent,
            new Game2LoadStageReadyMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2PlayAgainMessageEvent,
            new Game2PlayAgainMessageParser()
        );

        // Game Directory
        builder.MapParser(
            MessageEvent.Game2CheckGameDirectoryStatusMessageEvent,
            new Game2CheckGameDirectoryStatusMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2GetAccountGameStatusMessageEvent,
            new Game2GetAccountGameStatusMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2LeaveGameMessageEvent,
            new Game2LeaveGameMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2QuickJoinGameMessageEvent,
            new Game2QuickJoinGameMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2StartSnowWarMessageEvent,
            new Game2StartSnowWarMessageParser()
        );

        // Game Ingame
        builder.MapParser(
            MessageEvent.Game2MakeSnowballMessageEvent,
            new Game2MakeSnowballMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2RequestFullStatusUpdateMessageEvent,
            new Game2RequestFullStatusUpdateMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2SetUserMoveTargetMessageEvent,
            new Game2SetUserMoveTargetMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2ThrowSnowballAtHumanMessageEvent,
            new Game2ThrowSnowballAtHumanMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2ThrowSnowballAtPositionMessageEvent,
            new Game2ThrowSnowballAtPositionMessageParser()
        );

        // Game Lobby
        builder.MapParser(
            MessageEvent.GetResolutionAchievementsMessageEvent,
            new GetResolutionAchievementsMessageParser()
        );
        builder.MapParser(
            MessageEvent.ResetResolutionAchievementMessageEvent,
            new ResetResolutionAchievementMessageParser()
        );

        builder.MapSerializer(
            typeof(AchievementResolutionsMessageComposer),
            new AchievementResolutionsMessageComposerSerializer(
                MessageComposer.AchievementResolutionsMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(AchievementResolutionProgressMessageComposer),
            new AchievementResolutionProgressMessageComposerSerializer(
                MessageComposer.AchievementResolutionProgressMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(AchievementResolutionCompletedMessageComposer),
            new AchievementResolutionCompletedMessageComposerSerializer(
                MessageComposer.AchievementResolutionCompletedMessageComposer
            )
        );
        // builder.MapParser(MessageEvent.GetUserGameAchievementsMessageEvent, new GetUserGameAchievementsMessageParser());

        // Game Score
        builder.MapParser(
            MessageEvent.Game2GetFriendsLeaderboardEvent,
            new Game2GetFriendsLeaderboardMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2GetTotalGroupLeaderboardEvent,
            new Game2GetTotalGroupLeaderboardMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2GetTotalLeaderboardEvent,
            new Game2GetTotalLeaderboardMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2GetWeeklyFriendsLeaderboardEvent,
            new Game2GetWeeklyFriendsLeaderboardMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2GetWeeklyGroupLeaderboardEvent,
            new Game2GetWeeklyGroupLeaderboardMessageParser()
        );
        builder.MapParser(
            MessageEvent.Game2GetWeeklyLeaderboardEvent,
            new Game2GetWeeklyLeaderboardMessageParser()
        );
        // builder.MapParser(MessageEvent.GetFriendsWeeklyCompetitiveLeaderboardMessageEvent, new GetFriendsWeeklyCompetitiveLeaderboardMessageParser());
        // builder.MapParser(MessageEvent.GetWeeklyCompetitiveLeaderboardMessageEvent, new GetWeeklyCompetitiveLeaderboardMessageParser());
        // builder.MapParser(MessageEvent.GetWeeklyGameRewardMessageEvent, new GetWeeklyGameRewardMessageParser());
        // builder.MapParser(MessageEvent.GetWeeklyGameRewardWinnersMessageEvent, new GetWeeklyGameRewardWinnersMessageParser());
    }
}
