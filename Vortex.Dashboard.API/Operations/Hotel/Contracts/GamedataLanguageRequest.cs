using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

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
