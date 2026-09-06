using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteArticleCategoryRequest(int CategoryId, string Reason) : IReasonedRequest;
