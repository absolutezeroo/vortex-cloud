using System;
using System.Collections.Immutable;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Primitives.Signals;

/// <summary>
/// The vocabulary of things a player can be said to have done.
/// </summary>
/// <remarks>
/// <para>
/// Every constant here <em>forwards</em> to <see cref="RewardTrackActions"/> rather than repeating
/// its string. That is deliberate and it is the whole lesson of this subsystem: the last time this
/// vocabulary was written down in two places — the handlers and <c>RewardTrackActionFacts</c> — the
/// two drifted on nine actions and nobody noticed, because nothing compares two hand-kept lists. A
/// forwarding constant cannot drift; the compiler is the check.
/// </para>
/// <para>
/// The strings stay where their reason lives. They are the target client's artwork keys —
/// <c>RewardTrackTaskRowView</c> builds an icon name as
/// <c>"reward_track_tasks_" + actionType.toLowerCase()</c> — and <see cref="RewardTrackActions"/>
/// carries the full account of why inventing one leaves a blank square. This class exists so a
/// consumer that is not reward tracks does not have to name a reward-track type to say "a player
/// entered a room".
/// </para>
/// <para>
/// The list is <em>declared</em>, not derived from the translators that exist. Two of these actions
/// have no producer today (<see cref="Teleport"/>, <see cref="Wired"/>) and content may already name
/// them; deriving the list would make them vanish from the editor rather than be shown as inert,
/// and a task written on one would become invisible instead of being flagged.
/// </para>
/// </remarks>
public static class SignalActions
{
    /// <inheritdoc cref="RewardTrackActions.EnterOtherUsersRoom"/>
    public const string EnterOtherUsersRoom = RewardTrackActions.EnterOtherUsersRoom;

    /// <inheritdoc cref="RewardTrackActions.CreateRoom"/>
    public const string CreateRoom = RewardTrackActions.CreateRoom;

    /// <inheritdoc cref="RewardTrackActions.PlaceItem"/>
    public const string PlaceItem = RewardTrackActions.PlaceItem;

    /// <inheritdoc cref="RewardTrackActions.MoveItem"/>
    public const string MoveItem = RewardTrackActions.MoveItem;

    /// <inheritdoc cref="RewardTrackActions.RotateItem"/>
    public const string RotateItem = RewardTrackActions.RotateItem;

    /// <inheritdoc cref="RewardTrackActions.PickUpItem"/>
    public const string PickUpItem = RewardTrackActions.PickUpItem;

    /// <inheritdoc cref="RewardTrackActions.WalkOnFurni"/>
    public const string WalkOnFurni = RewardTrackActions.WalkOnFurni;

    /// <inheritdoc cref="RewardTrackActions.Teleport"/>
    public const string Teleport = RewardTrackActions.Teleport;

    /// <inheritdoc cref="RewardTrackActions.ChatWithSomeone"/>
    public const string ChatWithSomeone = RewardTrackActions.ChatWithSomeone;

    /// <inheritdoc cref="RewardTrackActions.RequestFriend"/>
    public const string RequestFriend = RewardTrackActions.RequestFriend;

    /// <inheritdoc cref="RewardTrackActions.GiveRespect"/>
    public const string GiveRespect = RewardTrackActions.GiveRespect;

    /// <inheritdoc cref="RewardTrackActions.SendMessengerMessage"/>
    public const string SendMessengerMessage = RewardTrackActions.SendMessengerMessage;

    /// <inheritdoc cref="RewardTrackActions.Dance"/>
    public const string Dance = RewardTrackActions.Dance;

    /// <inheritdoc cref="RewardTrackActions.Wave"/>
    public const string Wave = RewardTrackActions.Wave;

    /// <inheritdoc cref="RewardTrackActions.ChangeFigure"/>
    public const string ChangeFigure = RewardTrackActions.ChangeFigure;

    /// <inheritdoc cref="RewardTrackActions.ChangeMotto"/>
    public const string ChangeMotto = RewardTrackActions.ChangeMotto;

    /// <inheritdoc cref="RewardTrackActions.WearBadge"/>
    public const string WearBadge = RewardTrackActions.WearBadge;

    /// <inheritdoc cref="RewardTrackActions.BuyFromCatalogue"/>
    public const string BuyFromCatalogue = RewardTrackActions.BuyFromCatalogue;

    /// <inheritdoc cref="RewardTrackActions.PetLevel"/>
    public const string PetLevel = RewardTrackActions.PetLevel;

    /// <inheritdoc cref="RewardTrackActions.UseHabbicon"/>
    public const string UseHabbicon = RewardTrackActions.UseHabbicon;

    /// <inheritdoc cref="RewardTrackActions.CompleteTrade"/>
    public const string CompleteTrade = RewardTrackActions.CompleteTrade;

    /// <inheritdoc cref="RewardTrackActions.SpendCredits"/>
    public const string SpendCredits = RewardTrackActions.SpendCredits;

    /// <inheritdoc cref="RewardTrackActions.CompleteHabbiconCollection"/>
    public const string CompleteHabbiconCollection = RewardTrackActions.CompleteHabbiconCollection;

    /// <inheritdoc cref="RewardTrackActions.CompleteQuest"/>
    public const string CompleteQuest = RewardTrackActions.CompleteQuest;

    /// <inheritdoc cref="RewardTrackActions.AchievementLevel"/>
    public const string AchievementLevel = RewardTrackActions.AchievementLevel;

    /// <inheritdoc cref="RewardTrackActions.Wired"/>
    public const string Wired = RewardTrackActions.Wired;

    /// <summary>
    /// Every core action. The governance test freezes this list, and the translator processor uses
    /// it to tell a core action from one a plugin must qualify with its own key.
    /// </summary>
    public static readonly ImmutableHashSet<string> All = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        EnterOtherUsersRoom,
        CreateRoom,
        PlaceItem,
        MoveItem,
        RotateItem,
        PickUpItem,
        WalkOnFurni,
        Teleport,
        ChatWithSomeone,
        RequestFriend,
        GiveRespect,
        SendMessengerMessage,
        Dance,
        Wave,
        ChangeFigure,
        ChangeMotto,
        WearBadge,
        BuyFromCatalogue,
        PetLevel,
        UseHabbicon,
        CompleteTrade,
        SpendCredits,
        CompleteHabbiconCollection,
        CompleteQuest,
        AchievementLevel,
        Wired
    );
}
