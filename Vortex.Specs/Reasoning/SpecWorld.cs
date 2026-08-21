using System.Collections.Generic;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Analysis.Reference;
using Vortex.Specs.Captures;
using Vortex.Specs.Model;

namespace Vortex.Specs.Reasoning;

/// <summary>Everything every analyzer found, before any of it has been reconciled.</summary>
public sealed record SpecWorld
{
    public required EmulatorScan Emulator { get; init; }

    public required IReadOnlyList<ClientScan> Clients { get; init; }

    public required IReadOnlyList<ReferenceScan> References { get; init; }

    public required IReadOnlyList<CaptureDocument> Captures { get; init; }

    public required IReadOnlyList<CaptureObservation> Observations { get; init; }

    public required IReadOnlyList<TriggerSummary> TriggerSummaries { get; init; }

    /// <summary>Problems the readers hit, carried through so the report can show them.</summary>
    public required IReadOnlyList<string> Problems { get; init; }
}

/// <summary>
/// Translates one source's authority into the confidence a claim backed only by that source may
/// carry.
/// </summary>
/// <remarks>
/// The whole trustworthiness of the output rests on this staying a one-way street. Nothing else in
/// the pipeline is allowed to raise a confidence; the only way up the ladder is a second independent
/// source, which <see cref="Combine"/> handles.
/// </remarks>
public static class ConfidencePolicy
{
    public static Confidence FromAuthority(EvidenceAuthority authority) =>
        authority switch
        {
            EvidenceAuthority.OfficialCapture => Confidence.CaptureConfirmed,
            EvidenceAuthority.ClientMandated => Confidence.ClientConfirmed,
            EvidenceAuthority.ClientCode => Confidence.ClientConfirmed,
            EvidenceAuthority.MultiImplementation => Confidence.MultiReferenceConfirmed,
            EvidenceAuthority.ReferenceEmulator => Confidence.ReferenceObserved,
            EvidenceAuthority.VortexEmulator => Confidence.ImplementationObserved,
            EvidenceAuthority.Inference => Confidence.Inferred,
            _ => Confidence.Assumed,
        };

    /// <summary>
    /// The confidence a set of agreeing sources supports.
    /// </summary>
    /// <param name="agreeing">The authorities that agree on the claim.</param>
    /// <remarks>
    /// Two rules do all the work here. Vortex agreeing with a reference emulator is not two
    /// independent observations — this emulator was written partly by reading those — so Vortex is
    /// excluded from the count. And reaching <see cref="Confidence.Confirmed"/> needs two sources of
    /// official grade, which in practice means a capture plus the official client: no number of
    /// reimplementations agreeing with each other makes something official.
    /// </remarks>
    public static Confidence Combine(IReadOnlyCollection<EvidenceAuthority> agreeing)
    {
        if (agreeing.Count == 0)
        {
            return Confidence.Unknown;
        }

        int independent = 0;
        int officialGrade = 0;
        EvidenceAuthority best = EvidenceAuthority.Assumption;

        foreach (EvidenceAuthority authority in agreeing)
        {
            if (authority < best)
            {
                best = authority;
            }

            if (
                authority
                is EvidenceAuthority.VortexEmulator
                    or EvidenceAuthority.Inference
                    or EvidenceAuthority.Assumption
            )
            {
                continue;
            }

            independent++;

            if (authority <= EvidenceAuthority.ClientCode)
            {
                officialGrade++;
            }
        }

        if (officialGrade >= 2)
        {
            return Confidence.Confirmed;
        }

        Confidence ceiling = FromAuthority(best);

        // Two independent reimplementations agreeing is worth more than either alone, but never more
        // than the ladder allows for a reimplementation.
        if (independent >= 2 && ceiling < Confidence.MultiReferenceConfirmed)
        {
            return Confidence.MultiReferenceConfirmed;
        }

        return ceiling;
    }
}
