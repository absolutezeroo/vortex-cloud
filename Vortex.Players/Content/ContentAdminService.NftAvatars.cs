using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Content;
using Vortex.Primitives.Players.Avatar;

namespace Vortex.Players.Content;

/// <summary>
/// Writes for the whole avatars a player can wear, and for handing copies of them out.
/// <para>
/// This is the half of the feature the hotel actually runs on. Sulake's avatars are minted on a
/// chain and the server only ever reads what the wallet says; there is no wallet here, so the grant
/// written below <em>is</em> the ownership — which makes this the acquisition path, not a
/// convenience on top of one.
/// </para>
/// <para>
/// Nothing to reload: the wardrobe grain reads these tables per request rather than caching them, so
/// a grant is live the next time the player opens the avatar editor.
/// </para>
/// </summary>
internal sealed partial class ContentAdminService
{
    public async Task<ContentAdminResult> CreateNftAvatarAsync(
        NftAvatarSpec spec,
        CancellationToken ct
    )
    {
        if (ValidateNftAvatar(spec) is { } error)
        {
            return ContentAdminResult.Fail(error);
        }

        string code = spec.AvatarCode.Trim();

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (await db.NftAvatars.AnyAsync(a => a.AvatarCode == code, ct).ConfigureAwait(false))
        {
            return ContentAdminResult.Fail("avatar_code_taken");
        }

        NftAvatarEntity entity = new()
        {
            AvatarCode = code,
            Name = spec.Name.Trim(),
            Figure = spec.Figure.Trim(),
            Gender = NormalizeGender(spec.Gender),
            ContractKey = spec.ContractKey,
            EditionSize = spec.EditionSize,
            Enabled = spec.Enabled,
            SortOrder = spec.SortOrder,
        };

        db.NftAvatars.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> UpdateNftAvatarAsync(
        int avatarId,
        NftAvatarSpec spec,
        CancellationToken ct
    )
    {
        if (ValidateNftAvatar(spec) is { } error)
        {
            return ContentAdminResult.Fail(error);
        }

        string code = spec.AvatarCode.Trim();

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftAvatarEntity? entity = await db
            .NftAvatars.FirstOrDefaultAsync(a => a.Id == avatarId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("avatar_not_found");
        }

        if (
            await db
                .NftAvatars.AnyAsync(a => a.AvatarCode == code && a.Id != avatarId, ct)
                .ConfigureAwait(false)
        )
        {
            return ContentAdminResult.Fail("avatar_code_taken");
        }

        int handedOut = await db
            .PlayerNftAvatars.CountAsync(
                owned => owned.NftAvatarEntityId == avatarId && owned.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        // Shrinking an edition below what has already been given would put the run into a state its
        // own numbering contradicts -- "4 of 3". Nothing downstream checks for that, so it is
        // refused here.
        if (spec.EditionSize > 0 && spec.EditionSize < handedOut)
        {
            return ContentAdminResult.Fail("edition_below_granted");
        }

        entity.AvatarCode = code;
        entity.Name = spec.Name.Trim();
        entity.Figure = spec.Figure.Trim();
        entity.Gender = NormalizeGender(spec.Gender);
        entity.ContractKey = spec.ContractKey;
        entity.EditionSize = spec.EditionSize;
        entity.Enabled = spec.Enabled;
        entity.SortOrder = spec.SortOrder;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // A player wearing this avatar keeps the old figure until they change looks. Nothing pushes
        // the new one: the figure was copied onto them when they put it on, which is what lets them
        // walk around as it in the first place.
        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteNftAvatarAsync(int avatarId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftAvatarEntity? entity = await db
            .NftAvatars.FirstOrDefaultAsync(a => a.Id == avatarId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("avatar_not_found");
        }

        // Copies already given out point at this row and would be orphaned by a delete, so an avatar
        // anyone holds is unlisted rather than removed -- `enabled` is what the wardrobe reads.
        if (
            await db
                .PlayerNftAvatars.AnyAsync(
                    owned => owned.NftAvatarEntityId == avatarId && owned.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            return ContentAdminResult.Fail("avatar_granted_disable_instead");
        }

        db.NftAvatars.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(avatarId);
    }

    public async Task<ContentAdminResult> GrantNftAvatarAsync(
        int avatarId,
        int playerId,
        string note,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftAvatarEntity? avatar = await db
            .NftAvatars.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == avatarId && a.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (avatar is null)
        {
            return ContentAdminResult.Fail("avatar_not_found");
        }

        if (!await db.Players.AnyAsync(p => p.Id == playerId, ct).ConfigureAwait(false))
        {
            return ContentAdminResult.Fail("player_not_found");
        }

        PlayerNftAvatarEntity? existing = await db
            .PlayerNftAvatars.FirstOrDefaultAsync(
                owned => owned.NftAvatarEntityId == avatarId && owned.PlayerEntityId == playerId,
                ct
            )
            .ConfigureAwait(false);

        // Giving the same avatar twice would be a second tile showing the same character, so a
        // revoked copy is handed back instead -- keeping its number, which is the point of a number.
        if (existing is not null)
        {
            if (existing.DeletedAt is null)
            {
                return ContentAdminResult.Fail("avatar_already_held");
            }

            existing.DeletedAt = null;
            existing.GrantNote = note.Trim();
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return ContentAdminResult.Ok(existing.Id);
        }

        int granted = await db
            .PlayerNftAvatars.CountAsync(
                owned => owned.NftAvatarEntityId == avatarId && owned.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (avatar.EditionSize > 0 && granted >= avatar.EditionSize)
        {
            return ContentAdminResult.Fail("edition_exhausted");
        }

        // Counted over every copy ever cut, revoked ones included: a serial that came back into use
        // would name two holders in the run's history.
        int highestSerial =
            await db
                .PlayerNftAvatars.Where(owned => owned.NftAvatarEntityId == avatarId)
                .Select(owned => (int?)owned.SerialNumber)
                .MaxAsync(ct)
                .ConfigureAwait(false)
            ?? 0;

        PlayerNftAvatarEntity copy = new()
        {
            PlayerEntityId = playerId,
            NftAvatarEntityId = avatarId,
            SerialNumber = highestSerial + 1,
            GrantNote = note.Trim(),
        };

        db.PlayerNftAvatars.Add(copy);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(copy.Id);
    }

    public async Task<ContentAdminResult> RevokeNftAvatarAsync(int copyId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PlayerNftAvatarEntity? copy = await db
            .PlayerNftAvatars.FirstOrDefaultAsync(
                owned => owned.Id == copyId && owned.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (copy is null)
        {
            return ContentAdminResult.Fail("avatar_not_held");
        }

        // Taken off first if they are wearing it, otherwise they walk around as a character they no
        // longer own and the editor offers no tile to change back from.
        await db
            .PlayerNftOutfits.Where(row =>
                row.PlayerNftAvatarEntityId == copyId && row.DeletedAt == null
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        copy.DeletedAt = System.DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(copyId);
    }

    /// <summary>
    /// The client's own letter. Anything else and the avatar is drawn with the wrong body: the
    /// figure's parts are looked up per gender.
    /// </summary>
    private static string NormalizeGender(string gender) =>
        gender.Trim().Equals("F", System.StringComparison.OrdinalIgnoreCase) ? "F" : "M";

    private static string? ValidateNftAvatar(NftAvatarSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.AvatarCode))
        {
            return "avatar_code_required";
        }

        if (string.IsNullOrWhiteSpace(spec.Figure))
        {
            return "figure_required";
        }

        if (spec.Figure.Trim().Length > FigureString.MaxLength)
        {
            return "figure_too_long";
        }

        // The client switches on this string for the caption and the tile colours and has no branch
        // for anything else: an unknown collection draws the literal word "null" above the avatar.
        if (!NftAvatarCollection.IsKnown(spec.ContractKey))
        {
            return "unknown_collection";
        }

        return spec.EditionSize < 0 ? "edition_size_invalid" : null;
    }
}
