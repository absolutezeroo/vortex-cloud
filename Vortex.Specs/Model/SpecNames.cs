using System;
using System.Collections.Generic;

namespace Vortex.Specs.Model;

/// <summary>
/// The wire names for the enums above. Kept in one place because they are part of the on-disk
/// format: renaming a member must not silently rewrite every spec file.
/// </summary>
public static class SpecNames
{
    private static readonly Dictionary<Confidence, string> ConfidenceNames = new()
    {
        [Confidence.Unknown] = "unknown",
        [Confidence.Conflicting] = "conflicting",
        [Confidence.Assumed] = "assumed",
        [Confidence.Inferred] = "inferred",
        [Confidence.ImplementationObserved] = "implementation_observed",
        [Confidence.ReferenceObserved] = "reference_observed",
        [Confidence.MultiReferenceConfirmed] = "multi_reference_confirmed",
        [Confidence.ClientConfirmed] = "client_confirmed",
        [Confidence.CaptureConfirmed] = "capture_confirmed",
        [Confidence.Confirmed] = "confirmed",
    };

    private static readonly Dictionary<EvidenceAuthority, string> AuthorityNames = new()
    {
        [EvidenceAuthority.OfficialCapture] = "official_capture",
        [EvidenceAuthority.ClientMandated] = "client_mandated",
        [EvidenceAuthority.ClientCode] = "client_code",
        [EvidenceAuthority.MultiImplementation] = "multi_implementation",
        [EvidenceAuthority.ReferenceEmulator] = "reference_emulator",
        [EvidenceAuthority.VortexEmulator] = "vortex_emulator",
        [EvidenceAuthority.Inference] = "inference",
        [EvidenceAuthority.Assumption] = "assumption",
    };

    public static string Wire(this Confidence value) => ConfidenceNames[value];

    public static string Wire(this EvidenceAuthority value) => AuthorityNames[value];

    public static bool TryParseConfidence(string text, out Confidence value)
    {
        foreach (KeyValuePair<Confidence, string> pair in ConfidenceNames)
        {
            if (string.Equals(pair.Value, text, StringComparison.Ordinal))
            {
                value = pair.Key;
                return true;
            }
        }

        value = Confidence.Unknown;
        return false;
    }

    public static bool TryParseAuthority(string text, out EvidenceAuthority value)
    {
        foreach (KeyValuePair<EvidenceAuthority, string> pair in AuthorityNames)
        {
            if (string.Equals(pair.Value, text, StringComparison.Ordinal))
            {
                value = pair.Key;
                return true;
            }
        }

        value = EvidenceAuthority.Assumption;
        return false;
    }
}
