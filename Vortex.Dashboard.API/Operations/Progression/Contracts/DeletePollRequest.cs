using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeletePollRequest(int PollId, string Reason) : IReasonedRequest;
