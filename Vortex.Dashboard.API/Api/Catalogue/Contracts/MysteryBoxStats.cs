using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// What the boxes did over the window, and what is still in circulation.
/// </summary>
/// <param name="KeysOutstanding">A live count over the key table, not a windowed one: a key granted
/// last year and never spent is still in the economy today, which is the number that matters before
/// adding more.</param>
/// <param name="BoxesInCirculation">Boxes placed in rooms right now.</param>
public sealed record MysteryBoxStats(
    int Days,
    DateTime Since,
    int BoxesOpened,
    int TrophiesOpened,
    int PrizesAwarded,
    int KeysGranted,
    int KeysConsumed,
    int KeysOutstanding,
    IReadOnlyList<MysteryKeyColorCount> KeysByColor,
    int BoxesInCirculation
);
