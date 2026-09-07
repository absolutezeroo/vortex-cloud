using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>What the account did inside the window, by journal.</summary>
public sealed record ProfileTimeline(
    IReadOnlyList<ProfileEntryRow> Entries,
    IReadOnlyList<ProfileChatRow> Chats,
    IReadOnlyList<PlayerItemRow> Items
);
