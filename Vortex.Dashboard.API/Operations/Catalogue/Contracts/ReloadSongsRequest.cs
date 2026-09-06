using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record ReloadSongsRequest(string Reason) : IReasonedRequest;
