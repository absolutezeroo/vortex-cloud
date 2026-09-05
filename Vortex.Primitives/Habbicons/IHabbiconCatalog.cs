using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Vortex.Primitives.Habbicons.Snapshots;

namespace Vortex.Primitives.Habbicons;

/// <summary>
/// The Habbicon definitions and collections, cached in process. Reference data: a few hundred rows
/// read on every hub open, every purchase and every use, so it loads once with the other reference
/// caches rather than being queried per request.
/// </summary>
/// <remarks>
/// Read-only by construction. Content changes go through <c>IHabbiconAdminService</c>, which writes
/// the rows and then reloads this.
/// </remarks>
public interface IHabbiconCatalog
{
    /// <summary>Every collection, in display order, with its entries and its bonus Habbicon.</summary>
    ImmutableArray<HabbiconCollectionSnapshot> Collections { get; }

    /// <summary>One definition by id.</summary>
    bool TryGetHabbicon(
        int habbiconId,
        [NotNullWhen(true)] out HabbiconDefinitionSnapshot? definition
    );

    /// <summary>One collection by id.</summary>
    bool TryGetCollection(
        int collectionId,
        [NotNullWhen(true)] out HabbiconCollectionSnapshot? collection
    );

    /// <summary>The collection a Habbicon belongs to, bonus Habbicons included.</summary>
    bool TryGetCollectionOf(
        int habbiconId,
        [NotNullWhen(true)] out HabbiconCollectionSnapshot? collection
    );
}
