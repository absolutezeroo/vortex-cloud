using System;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Primitives.Hosting;
using Xunit;

namespace Vortex.Hosting.Tests;

/// <summary>
/// A web host that an operator switched on and that then failed to start used to be logged and
/// forgotten: the emulator carried on announcing itself as started, with a dead port nobody noticed
/// until a player or an admin tried to use it. These tests pin the three outcomes.
/// </summary>
public sealed class RequiredServiceGuardTests
{
    private const string SERVICE = "Vortex web API";
    private const string REQUIRED_OPTION = "Vortex:WebApi:Required";

    [Fact]
    public void ADisabledService_IsNotAFailureAtAll()
    {
        (RequiredServiceGuard guard, FakeLifetime lifetime) = Create();

        ServiceFailureAction action = guard.ReportStartupFailure(
            SERVICE,
            enabled: false,
            required: false,
            new InvalidOperationException("boom"),
            REQUIRED_OPTION
        );

        action.Should().Be(ServiceFailureAction.Ignore);
        lifetime.StopRequested.Should().BeFalse();
        guard.IsDegraded.Should().BeFalse();
    }

    [Fact]
    public void AnEnabledOptionalServiceThatFails_DegradesButKeepsTheHostRunning()
    {
        (RequiredServiceGuard guard, FakeLifetime lifetime) = Create();

        ServiceFailureAction action = guard.ReportStartupFailure(
            SERVICE,
            enabled: true,
            required: false,
            new InvalidOperationException("boom"),
            REQUIRED_OPTION
        );

        action.Should().Be(ServiceFailureAction.Degrade);
        lifetime.StopRequested.Should().BeFalse();
        guard.IsDegraded.Should().BeTrue();
        guard.DegradedServices.Should().Contain(SERVICE);
    }

    [Fact]
    public void AnEnabledRequiredServiceThatFails_StopsTheHostWithANonZeroExitCode()
    {
        (RequiredServiceGuard guard, FakeLifetime lifetime) = Create();

        int originalExitCode = Environment.ExitCode;

        try
        {
            ServiceFailureAction action = guard.ReportStartupFailure(
                SERVICE,
                enabled: true,
                required: true,
                new InvalidOperationException("boom"),
                REQUIRED_OPTION
            );

            action.Should().Be(ServiceFailureAction.FailHost);
            lifetime.StopRequested.Should().BeTrue();
            Environment
                .ExitCode.Should()
                .NotBe(0, "a supervisor must see a failure, not a clean shutdown");
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }
    }

    [Fact]
    public void AServiceThatLaterStarts_IsNoLongerDegraded()
    {
        (RequiredServiceGuard guard, _) = Create();

        guard.ReportStartupFailure(
            SERVICE,
            enabled: true,
            required: false,
            new InvalidOperationException("boom"),
            REQUIRED_OPTION
        );

        guard.ReportStarted(SERVICE);

        guard.IsDegraded.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false, ServiceFailureAction.Ignore)]
    [InlineData(false, true, ServiceFailureAction.Ignore)]
    [InlineData(true, false, ServiceFailureAction.Degrade)]
    [InlineData(true, true, ServiceFailureAction.FailHost)]
    public void ThePolicy_IsDrivenOnlyByEnabledAndRequired(
        bool enabled,
        bool required,
        ServiceFailureAction expected
    ) => RequiredServiceGuard.DecideAction(enabled, required).Should().Be(expected);

    private static (RequiredServiceGuard, FakeLifetime) Create()
    {
        FakeLifetime lifetime = new();

        return (
            new RequiredServiceGuard(lifetime, NullLogger<RequiredServiceGuard>.Instance),
            lifetime
        );
    }

    private sealed class FakeLifetime : IHostApplicationLifetime
    {
        public bool StopRequested { get; private set; }

        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() => StopRequested = true;
    }
}
