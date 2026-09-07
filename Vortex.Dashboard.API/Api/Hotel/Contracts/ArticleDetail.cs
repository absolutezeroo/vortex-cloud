using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One article with every translation it has, which is what the editor loads.</summary>
public sealed record ArticleDetail(
    int Id,
    string Slug,
    string Category,
    string Status,
    DateTime? PublishAt,
    bool Pinned,
    string Author,
    IReadOnlyList<ArticleTranslation> Translations
);
