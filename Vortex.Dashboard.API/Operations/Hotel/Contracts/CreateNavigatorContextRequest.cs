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
