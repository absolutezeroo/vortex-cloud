using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record BadgeGrantRequest(int PlayerId, string BadgeCode, string Reason)
    : IReasonedRequest;
