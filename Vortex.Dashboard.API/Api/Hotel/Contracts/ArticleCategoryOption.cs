namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One category an article may sit in. <c>Labels</c> is the stored per-language JSON.</summary>
public sealed record ArticleCategoryOption(
    int Id,
    string Code,
    string Labels,
    int SortOrder,
    bool Enabled
);
