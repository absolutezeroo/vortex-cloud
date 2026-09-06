using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteHandItemRequest(int Id, string Reason) : IReasonedRequest;
