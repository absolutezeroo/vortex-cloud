using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for editing the four gamedata files the client downloads.
/// </summary>
/// <remarks>
/// <c>File</c> is a token looked up in <c>GamedataDocumentStore.Files</c>, never a path: it comes off
/// the network, and a filename built from it by concatenation is the traversal bug rather than a
/// guard against one.
/// <para>
/// <c>ExpectedModifiedUtc</c> is what the page believed the file's write time to be when it loaded.
/// Several people touch a hotel, and an edit silently dropped from a file of 55 836 entries is not
/// something anybody notices.
/// </para>
/// </remarks>
public sealed record GamedataEntryRequest(
    string File,
    string? Language,
    string Key,
    string Value,
    DateTime? ExpectedModifiedUtc,
    string Reason
) : IReasonedRequest;
