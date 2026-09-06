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

public sealed record GamedataEntryDeleteRequest(
    string File,
    string? Language,
    string Key,
    DateTime? ExpectedModifiedUtc,
    string Reason
) : IReasonedRequest;

/// <summary>
/// One furnidata entry, addressed by its position.
/// </summary>
/// <remarks>
/// Not by <c>id</c> and not by <c>classname</c>: 55 836 entries carry only 55 254 distinct ids and
/// 51 425 distinct classnames. 577 ids are shared between the floor and wall lists — two namespaces,
/// legitimate — but 5 are duplicated inside <c>roomitemtypes</c> itself, and nothing says which one
/// the client keeps. The position is the only thing that identifies a row, which is also why
/// deleting one is not offered: every index after it would shift.
/// </remarks>
public sealed record GamedataFurniRequest(
    string Kind,
    int Index,
    string Field,
    string Value,
    DateTime? ExpectedModifiedUtc,
    string Reason
) : IReasonedRequest;

/// <summary>
/// Declares a language to the client, or withdraws it.
/// </summary>
/// <remarks>
/// Enabling writes the <c>localization.&lt;k&gt;</c> block into <c>external_variables.json</c> and
/// creates <c>gamedata/&lt;code&gt;/external_flash_texts.json</c> from the default language.
/// Disabling removes the block only: the translation work stays on disk, because losing it to a
/// misclick is not a recoverable kind of mistake.
/// </remarks>
public sealed record GamedataLanguageRequest(string Code, string Name, string Reason)
    : IReasonedRequest;

public sealed record GamedataLanguageRemoveRequest(string Code, string Reason) : IReasonedRequest;
