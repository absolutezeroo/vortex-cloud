using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteCollectionItemRequest(int ItemId, string Reason) : IReasonedRequest;
