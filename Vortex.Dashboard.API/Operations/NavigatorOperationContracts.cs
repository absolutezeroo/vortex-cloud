using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the navigator configuration operations, each carrying a mandatory audited
/// <c>Reason</c>. <c>QueryType</c> is the <c>NavigatorQueryType</c> ordinal — what the tab or block
/// actually searches — and <c>SearchCode</c> is the client's own code for it.
/// </summary>
public sealed record CreateNavigatorContextRequest(
    string SearchCode,
    bool Visible,
    int QueryType,
    int OrderNum,
    string Reason
) : IReasonedRequest;

public sealed record UpdateNavigatorContextRequest(
    int ContextId,
    string SearchCode,
    bool Visible,
    int QueryType,
    int OrderNum,
    string Reason
) : IReasonedRequest;

public sealed record DeleteNavigatorContextRequest(int ContextId, string Reason) : IReasonedRequest;

public sealed record CreateNavigatorQuickLinkRequest(
    int ContextId,
    string SearchCode,
    string Filter,
    string Localization,
    int QueryType,
    int OrderNum,
    string Reason
) : IReasonedRequest;

public sealed record UpdateNavigatorQuickLinkRequest(
    int QuickLinkId,
    int ContextId,
    string SearchCode,
    string Filter,
    string Localization,
    int QueryType,
    int OrderNum,
    string Reason
) : IReasonedRequest;

public sealed record DeleteNavigatorQuickLinkRequest(int QuickLinkId, string Reason)
    : IReasonedRequest;

public sealed record CreateNavigatorFlatCategoryRequest(
    string Name,
    bool Visible,
    bool Automatic,
    string? AutomaticCategory,
    string? GlobalCategory,
    bool StaffOnly,
    int MinRank,
    int OrderNum,
    string Reason
) : IReasonedRequest;

public sealed record UpdateNavigatorFlatCategoryRequest(
    int CategoryId,
    string Name,
    bool Visible,
    bool Automatic,
    string? AutomaticCategory,
    string? GlobalCategory,
    bool StaffOnly,
    int MinRank,
    int OrderNum,
    string Reason
) : IReasonedRequest;

public sealed record DeleteNavigatorFlatCategoryRequest(int CategoryId, string Reason)
    : IReasonedRequest;

public sealed record CreateNavigatorEventCategoryRequest(string Name, bool Visible, string Reason)
    : IReasonedRequest;

public sealed record UpdateNavigatorEventCategoryRequest(
    int CategoryId,
    string Name,
    bool Visible,
    string Reason
) : IReasonedRequest;

public sealed record DeleteNavigatorEventCategoryRequest(int CategoryId, string Reason)
    : IReasonedRequest;

public sealed record SeedNavigatorDefaultsRequest(string Reason) : IReasonedRequest;
