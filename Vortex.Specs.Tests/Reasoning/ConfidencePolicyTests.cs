using System.Linq;
using FluentAssertions;
using Vortex.Specs.Model;
using Vortex.Specs.Reasoning;
using Xunit;

namespace Vortex.Specs.Tests.Reasoning;

/// <summary>
/// The rules that decide how much a claim is worth. Every other guarantee in this system rests on
/// these staying a one-way street, so they are pinned individually rather than exercised
/// incidentally through the pipeline.
/// </summary>
public class ConfidencePolicyTests
{
    [Theory]
    [InlineData(EvidenceAuthority.OfficialCapture, Confidence.CaptureConfirmed)]
    [InlineData(EvidenceAuthority.ClientMandated, Confidence.ClientConfirmed)]
    [InlineData(EvidenceAuthority.ClientCode, Confidence.ClientConfirmed)]
    [InlineData(EvidenceAuthority.MultiImplementation, Confidence.MultiReferenceConfirmed)]
    [InlineData(EvidenceAuthority.ReferenceEmulator, Confidence.ReferenceObserved)]
    [InlineData(EvidenceAuthority.VortexEmulator, Confidence.ImplementationObserved)]
    [InlineData(EvidenceAuthority.Inference, Confidence.Inferred)]
    [InlineData(EvidenceAuthority.Assumption, Confidence.Assumed)]
    public void One_source_never_gets_more_than_its_own_authority(
        EvidenceAuthority authority,
        Confidence expected
    )
    {
        ConfidencePolicy.FromAuthority(authority).Should().Be(expected);
        ConfidencePolicy.Combine([authority]).Should().Be(expected);
    }

    [Fact]
    public void Nothing_backing_a_claim_leaves_it_unknown()
    {
        ConfidencePolicy.Combine([]).Should().Be(Confidence.Unknown);
    }

    [Fact]
    public void Vortex_agreeing_with_a_reference_is_not_two_independent_sources()
    {
        // This emulator was written in part by reading those implementations. Counting it as
        // corroboration would let a mistake copied from Arcturus promote itself to
        // multi_reference_confirmed, which is the exact failure this rule exists to prevent.
        ConfidencePolicy
            .Combine([EvidenceAuthority.VortexEmulator, EvidenceAuthority.ReferenceEmulator])
            .Should()
            .Be(Confidence.ReferenceObserved);
    }

    [Fact]
    public void Two_independent_reimplementations_agreeing_reach_multi_reference()
    {
        ConfidencePolicy
            .Combine([EvidenceAuthority.ReferenceEmulator, EvidenceAuthority.MultiImplementation])
            .Should()
            .Be(Confidence.MultiReferenceConfirmed);
    }

    [Fact]
    public void No_number_of_reimplementations_reaches_confirmed()
    {
        ConfidencePolicy
            .Combine([
                EvidenceAuthority.ReferenceEmulator,
                EvidenceAuthority.ReferenceEmulator,
                EvidenceAuthority.MultiImplementation,
                EvidenceAuthority.VortexEmulator,
            ])
            .Should()
            .Be(Confidence.MultiReferenceConfirmed);
    }

    [Fact]
    public void Confirmed_needs_two_official_grade_sources()
    {
        ConfidencePolicy
            .Combine([EvidenceAuthority.OfficialCapture, EvidenceAuthority.ClientCode])
            .Should()
            .Be(Confidence.Confirmed);

        // One official-grade source on its own does not.
        ConfidencePolicy
            .Combine([EvidenceAuthority.ClientCode, EvidenceAuthority.ReferenceEmulator])
            .Should()
            .Be(Confidence.ClientConfirmed);
    }

    [Fact]
    public void An_inference_never_lifts_anything()
    {
        ConfidencePolicy
            .Combine([
                EvidenceAuthority.VortexEmulator,
                EvidenceAuthority.Inference,
                EvidenceAuthority.Assumption,
            ])
            .Should()
            .Be(Confidence.ImplementationObserved);
    }

    [Fact]
    public void Confidence_levels_are_ordered_weakest_to_strongest()
    {
        // The ordering is load-bearing: the validator compares levels numerically to catch a claim
        // that outranks its evidence.
        Confidence[] ladder =
        [
            Confidence.Unknown,
            Confidence.Conflicting,
            Confidence.Assumed,
            Confidence.Inferred,
            Confidence.ImplementationObserved,
            Confidence.ReferenceObserved,
            Confidence.MultiReferenceConfirmed,
            Confidence.ClientConfirmed,
            Confidence.CaptureConfirmed,
            Confidence.Confirmed,
        ];

        ladder.Select(level => (int)level).Should().BeInAscendingOrder();
    }

    [Theory]
    [InlineData(Confidence.Unknown, "unknown")]
    [InlineData(Confidence.ImplementationObserved, "implementation_observed")]
    [InlineData(Confidence.MultiReferenceConfirmed, "multi_reference_confirmed")]
    [InlineData(Confidence.Confirmed, "confirmed")]
    public void Wire_names_round_trip(Confidence level, string wire)
    {
        level.Wire().Should().Be(wire);
        SpecNames.TryParseConfidence(wire, out Confidence parsed).Should().BeTrue();
        parsed.Should().Be(level);
    }
}
