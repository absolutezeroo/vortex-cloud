using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record DeleteArticleTranslationRequest(int ArticleId, string Lang, string Reason)
    : IReasonedRequest;
