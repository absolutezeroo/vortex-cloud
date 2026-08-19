using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Moderation;

/// <summary>
/// Backs the staff <c>:uc</c> / <c>:anew</c> chat commands, which paint a label next to the players
/// in a room (or across the hotel) that match a classification.
/// </summary>
public interface IUserClassificationService
{
    /// <summary>
    /// Labels whichever of <paramref name="playerIds"/> match <paramref name="classification"/>.
    /// Players that do not match are left out entirely — the client lists exactly what it is given.
    /// An unrecognised classification returns nothing rather than falling back to some default: the
    /// client accepts free text, so a typo must read as "no matches", not as another query.
    /// </summary>
    /// <param name="newUserWindowDays">How recently a player must have registered to count as new.
    /// Passed in rather than read here so the tunable stays in one admin-editable place.</param>
    Task<ImmutableArray<UserClassificationEntry>> ClassifyAsync(
        IReadOnlyCollection<int> playerIds,
        string classification,
        int newUserWindowDays,
        CancellationToken ct = default
    );
}
