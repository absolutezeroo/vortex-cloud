using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteArticleRequest(int ArticleId, string Reason) : IReasonedRequest;
