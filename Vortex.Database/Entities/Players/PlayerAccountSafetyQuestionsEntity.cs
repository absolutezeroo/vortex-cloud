using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// The two security questions guarding an account, and the hashes of their answers. A row here IS
/// the account's protection: habbo.com's "Protection du compte" reads as enabled exactly when one
/// exists, and disabling the feature deletes it.
/// </summary>
/// <remarks>
/// <para>
/// A table of its own rather than four more columns on <c>player_accounts</c>. That row is the
/// credential row — every sign-in, every session resolve and every spending guard reads it, usually
/// with a projection over one or two columns — and two 255-character hashes belong to a path that
/// runs once in a blue moon, when a visitor is challenged. Keeping them apart also means the answers
/// can be loaded only by the code that verifies them, rather than travelling with every account
/// read. <see cref="PlayerAccountPreferencesEntity" /> is the same shape for the same reason.
/// </para>
/// <para>
/// The questions are stored as their NUMBER, 1 to 9, because that is what they are on the website:
/// `IDENTITY_SAFETYQUESTION_1` … `_9` are localisation keys, so the question's wording belongs to
/// whichever language the visitor is reading and must not be frozen into the database. The pair is
/// always two DIFFERENT questions — habbo.com's own select filters each one out of the other's list.
/// </para>
/// <para>
/// The answers are BCrypt hashes, at the same work factor as a password, and they are hashed after
/// the same normalisation the verify path applies. They are a secret a player types, so they are
/// never stored in a form that could be read back.
/// </para>
/// </remarks>
[Table("player_account_safety_questions")]
[Index(nameof(PlayerAccountEntityId), IsUnique = true)]
public class PlayerAccountSafetyQuestionsEntity : VortexEntity
{
    [Column("account_id")]
    public required int PlayerAccountEntityId { get; set; }

    [Column("question_1")]
    public required int Question1 { get; set; }

    [Column("answer_1_hash")]
    [StringLength(255)]
    public required string Answer1Hash { get; set; }

    [Column("question_2")]
    public required int Question2 { get; set; }

    [Column("answer_2_hash")]
    [StringLength(255)]
    public required string Answer2Hash { get; set; }

    [ForeignKey(nameof(PlayerAccountEntityId))]
    public PlayerAccountEntity? PlayerAccount { get; set; }
}
