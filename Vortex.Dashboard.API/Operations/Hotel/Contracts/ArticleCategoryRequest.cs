using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary><paramref name="Labels"/> is a per-language dictionary, e.g.
/// <c>{"fr":"Campagnes","en":"Campaigns"}</c>.</summary>
public sealed record ArticleCategoryRequest(
    int CategoryId,
    string Code,
    string Labels,
    int SortOrder,
    bool Enabled,
    string Reason
) : IReasonedRequest;
