using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Vortex.Database.Entities.Room;
using Vortex.Logging.Extensions;
using Vortex.Primitives.Action;
using Vortex.Primitives.Bots;
using Vortex.Protocol.Messages.Outgoing.Room.Action;
using Vortex.Protocol.Messages.Outgoing.Room.Bots;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// The buttons on a bot's menu. The client draws one per skill id the server says the bot carries,
/// so this file is where "what a bot can be told to do" is decided; <see cref="BotSkillId"/> holds
/// the numbers the client reads them by.
/// </summary>
public sealed partial class RoomBotSystem
{
    /// <summary>
    /// What this server can actually carry out, which is deliberately narrower than what the client
    /// can draw. Announcing a skill nobody implements gives the owner a button that does nothing.
    /// </summary>
    private static readonly ImmutableArray<short> SupportedSkillIds =
    [
        BotSkillId.DressUp,
        BotSkillId.Chatter,
        BotSkillId.RandomWalk,
        BotSkillId.Dance,
        BotSkillId.ChangeName,
    ];

    /// <summary>Fits the name column with room to spare, and matches what the client's box takes.</summary>
    private const int MaxBotNameLength = 32;

    private const string FlagOn = "1";
    private const string FlagOff = "0";

    /// <summary>
    /// Applies one configured skill to the bot, in place. Returns false to refuse the change, which
    /// leaves the row untouched.
    /// <para>
    /// Anything unrecognised is stored verbatim rather than refused, so a skill this server has
    /// never heard of still round-trips through the dialog that wrote it.
    /// </para>
    /// </summary>
    private bool TryApplySkill(
        ActionContext ctx,
        BotEntity bot,
        int commandId,
        string data,
        Dictionary<string, string> skills
    )
    {
        string key = commandId.ToString(CultureInfo.InvariantCulture);

        switch (commandId)
        {
            case BotSkillId.DressUp:
                return TryDressUp(ctx, bot);

            case BotSkillId.ChangeName:
                return TryRename(bot, data);

            // Both buttons are plain toggles — the client sends empty data on every click and shows
            // no state — so the flag they flip has to live here.
            case BotSkillId.RandomWalk:
            case BotSkillId.Dance:
                skills[key] = IsFlagOn(skills.GetValueOrDefault(key, string.Empty))
                    ? FlagOff
                    : FlagOn;

                return true;

            default:
                skills[key] = data;

                return true;
        }
    }

    /// <summary>
    /// The bot copies the look its owner is wearing, taken from the avatar standing in this room
    /// rather than from the player's stored figure: what the owner sees on screen is what they are
    /// asking the bot to wear, and a wardrobe change that has not been saved is still on screen.
    /// </summary>
    private bool TryDressUp(ActionContext ctx, BotEntity bot)
    {
        if (
            !_roomGrain._state.AvatarsByPlayerId.TryGetValue(
                ctx.PlayerId,
                out RoomObjectId objectId
            )
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
            || avatar is not IRoomPlayer player
            || string.IsNullOrWhiteSpace(player.Figure)
        )
        {
            return false;
        }

        bot.Figure = player.Figure;
        bot.Gender = player.Gender;

        return true;
    }

    private static bool TryRename(BotEntity bot, string data)
    {
        string name = data.Trim();

        if (name.Length == 0 || name.Length > MaxBotNameLength)
        {
            return false;
        }

        bot.Name = name;

        return true;
    }

    /// <summary>
    /// Redraws a bot whose appearance or name changed. The room already knows how to do this for a
    /// player who changes clothes, and the client reads the same block for either.
    /// </summary>
    private void BroadcastLook(BotSnapshot bot) =>
        _roomGrain
            .SendComposerToRoomAsync(
                new UserChangeMessageComposer
                {
                    ObjectId = ToRoomObjectId(bot.BotId),
                    Figure = bot.Figure,
                    Gender = bot.Gender,
                    CustomInfo = bot.Motto,
                    AchievementScore = 0,
                }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish bot look change in room {RoomId}",
                _roomGrain._state.RoomId
            );

    private void BroadcastDance(int botId, bool dancing) =>
        _roomGrain
            .SendComposerToRoomAsync(
                new DanceMessageComposer
                {
                    ObjectId = ToRoomObjectId(botId),
                    // The menu has one dance button, so it is the plain dance rather than one of
                    // the three the client can also draw.
                    DanceType = dancing ? AvatarDanceType.Dance : AvatarDanceType.None,
                }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish bot dance in room {RoomId}",
                _roomGrain._state.RoomId
            );

    /// <summary>
    /// A skill list as the client's menu wants it: every skill the server supports, carrying its
    /// stored configuration where there is one. A supported-but-unconfigured skill still has to
    /// appear or its button is never drawn.
    /// </summary>
    private static ImmutableArray<BotSkillEntry> BuildSkillEntries(
        Dictionary<string, string> skills
    ) =>
        [
            .. SupportedSkillIds.Select(id => new BotSkillEntry
            {
                CommandId = id,
                Data = skills.GetValueOrDefault(
                    id.ToString(CultureInfo.InvariantCulture),
                    string.Empty
                ),
            }),
        ];

    /// <summary>Tolerates the client's own "true" spelling as well as the flag this writes.</summary>
    private static bool IsFlagOn(string value)
    {
        string trimmed = value.Trim();

        return trimmed == FlagOn || trimmed.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDancing(int botId) =>
        _skillsByBotId.TryGetValue(botId, out Dictionary<string, string>? skills)
        && IsFlagOn(
            skills.GetValueOrDefault(
                BotSkillId.Dance.ToString(CultureInfo.InvariantCulture),
                string.Empty
            )
        );
}
