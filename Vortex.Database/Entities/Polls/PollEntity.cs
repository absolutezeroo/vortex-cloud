using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Polls;

/// <summary>
/// A survey offered to a player inside a room: the client shows <see cref="Headline"/> /
/// <see cref="Summary"/> in the offer dialog ("Frank" prompt), then — if accepted — walks the
/// player through <see cref="PollQuestionEntity"/> rows between <see cref="StartMessage"/> and
/// <see cref="EndMessage"/>.
/// </summary>
[Table("polls")]
[Index(nameof(Code), IsUnique = true)]
public class PollEntity : VortexEntity
{
    /// <summary>Stable admin handle for the poll (not sent to the client).</summary>
    [Column("code")]
    public required string Code { get; set; }

    /// <summary>
    /// Free-form kind sent as the offer's <c>type</c> field. The client stores it and does not
    /// branch on it; it exists so an operator can tag surveys ("nps", "feedback", ...).
    /// </summary>
    [Column("poll_type")]
    [DefaultValue("")]
    public string PollType { get; set; } = string.Empty;

    /// <summary>Title line of the offer dialog.</summary>
    [Column("headline")]
    public required string Headline { get; set; }

    /// <summary>Body text of the offer dialog.</summary>
    [Column("summary")]
    public required string Summary { get; set; }

    /// <summary>Text shown above the first question.</summary>
    [Column("start_message")]
    [DefaultValue("")]
    public string StartMessage { get; set; } = string.Empty;

    /// <summary>Text shown on the thank-you card after the last question.</summary>
    [Column("end_message")]
    [DefaultValue("")]
    public string EndMessage { get; set; } = string.Empty;

    /// <summary>
    /// When true the client runs the branching flow: after a root answer it looks for a child
    /// question whose <see cref="PollQuestionEntity.QuestionCategory"/> equals the picked choice's
    /// <see cref="PollQuestionChoiceEntity.ChoiceType"/>. When false, children are never shown.
    /// </summary>
    [Column("nps_poll")]
    [DefaultValue(false)]
    public bool NpsPoll { get; set; }

    /// <summary>When false the poll is never offered and never served (disable without deleting).</summary>
    [Column("enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    /// <summary>When true the poll is offered automatically the first time a player enters a room.</summary>
    [Column("offer_on_room_entry")]
    [DefaultValue(true)]
    public bool OfferOnRoomEntry { get; set; } = true;

    /// <summary>
    /// Restricts the offer to one room; null = any room. Deliberately a plain id with no foreign
    /// key: deleting the room should retire the offer, not cascade into the survey and its results.
    /// A poll pinned to a room that no longer exists simply never matches.
    /// </summary>
    [Column("room_id")]
    public int? RoomEntityId { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }
}
