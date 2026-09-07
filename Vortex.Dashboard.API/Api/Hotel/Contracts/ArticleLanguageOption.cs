namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One language the site publishes in.</summary>
public sealed record ArticleLanguageOption(
    int Id,
    string Code,
    string Label,
    bool IsDefault,
    bool Enabled,
    int SortOrder
);
