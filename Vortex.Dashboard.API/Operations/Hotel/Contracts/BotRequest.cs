using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record BotRequest(int BotId, string Name, string Motto, string Figure, string Reason)
    : IReasonedRequest;
