using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Supervisor;
using Vortex.Supervisor.Configuration;
using Xunit;

namespace Vortex.Supervisor.Tests;

/// <summary>
/// This listener can stop the hotel and stream its console, and it answers while everything else is
/// down — so the two ways it can be handed to a stranger (the shipped placeholder secret, a
/// cleartext off-box bind) are refused at startup rather than warned about.
/// </summary>
public sealed class SupervisorSecurityTests
{
    private const string GOOD_TOKEN = "b8c1d0a4f27e4c1e9a3b5d6f7081a2b3";

    [Fact]
    public void TheShippedPlaceholderToken_IsRefused()
    {
        ValidateOptionsResult result = Validate(Config(token: SupervisorConfig.PLACEHOLDER_TOKEN));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("placeholder");
    }

    [Theory]
    [InlineData("CHANGE_ME")]
    [InlineData("CHANGE_ME_please_this_is_long_enough_to_pass_length")]
    public void AnyTokenStillCarryingTheChangeMeMarker_IsRefused(string token) =>
        Validate(Config(token: token)).Failed.Should().BeTrue();

    /// <summary>
    /// A token set in appsettings.Development.json while the process runs as Production is never
    /// layered on, and the refusal used to read as "you did not set it" for a value the operator
    /// demonstrably had set. The message has to name the environment it actually read.
    /// </summary>
    [Fact]
    public void ThePlaceholderRefusal_NamesTheEnvironmentItRead()
    {
        ValidateOptionsResult result = Validate(
            Config(token: SupervisorConfig.PLACEHOLDER_TOKEN),
            environmentName: "Production"
        );

        result.FailureMessage.Should().Contain("Production");
        result.FailureMessage.Should().Contain("appsettings.Production.json");
    }

    [Fact]
    public void AnEmptyToken_IsRefused() => Validate(Config(token: "")).Failed.Should().BeTrue();

    [Fact]
    public void AShortToken_IsRefused() =>
        Validate(Config(token: "abc123")).Failed.Should().BeTrue();

    [Fact]
    public void AWildcardBindOverPlainHttp_IsRefused()
    {
        ValidateOptionsResult result = Validate(Config(host: "0.0.0.0"));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("cleartext");
    }

    [Fact]
    public void ARemoteBindOverPlainHttp_IsRefused() =>
        Validate(Config(host: "192.168.1.10")).Failed.Should().BeTrue();

    [Fact]
    public void ARemoteBind_IsAllowedWhenExplicitlyAcceptedInWriting() =>
        Validate(Config(host: "0.0.0.0", allowInsecure: true)).Succeeded.Should().BeTrue();

    [Fact]
    public void TheDefaultLoopbackSetupWithARealToken_Passes() =>
        Validate(Config()).Succeeded.Should().BeTrue();

    [Fact]
    public void AnOutOfRangePort_IsRefused() =>
        Validate(Config(port: 70000)).Failed.Should().BeTrue();

    // ── Token comparison ─────────────────────────────────────────────────────

    [Fact]
    public void TheRightToken_Matches() =>
        SupervisorAuth.TokenMatches(GOOD_TOKEN, GOOD_TOKEN).Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("b8c1d0a4f27e4c1e9a3b5d6f7081a2b4")]
    [InlineData("b8c1d0a4f27e4c1e9a3b5d6f7081a2b")]
    [InlineData("b8c1d0a4f27e4c1e9a3b5d6f7081a2b33")]
    public void AnythingElse_DoesNot(string? presented) =>
        SupervisorAuth.TokenMatches(presented, GOOD_TOKEN).Should().BeFalse();

    [Fact]
    public void TheBearerHeader_IsRead()
    {
        DefaultHttpContext http = new();
        http.Request.Headers.Authorization = $"Bearer {GOOD_TOKEN}";

        SupervisorAuth.ExtractToken(http.Request).Should().Be(GOOD_TOKEN);
    }

    [Fact]
    public void TheBearerScheme_IsCaseInsensitive()
    {
        DefaultHttpContext http = new();
        http.Request.Headers.Authorization = $"bearer {GOOD_TOKEN}";

        SupervisorAuth.ExtractToken(http.Request).Should().Be(GOOD_TOKEN);
    }

    /// <summary>
    /// The browser's EventSource cannot set headers, so the console stream authenticates by cookie —
    /// which is why the cookie has to be read here and not only the header.
    /// </summary>
    [Fact]
    public void TheCookie_IsReadWhenNoHeaderIsPresent()
    {
        DefaultHttpContext http = new();
        http.Request.Headers.Cookie = $"{SupervisorAuth.CookieName}={GOOD_TOKEN}";

        SupervisorAuth.ExtractToken(http.Request).Should().Be(GOOD_TOKEN);
    }

    [Fact]
    public void NoCredentialAtAll_ReadsAsNull() =>
        SupervisorAuth.ExtractToken(new DefaultHttpContext().Request).Should().BeNull();

    private static ValidateOptionsResult Validate(
        SupervisorConfig config,
        string environmentName = "Production"
    ) => new SupervisorConfigValidator(new FakeEnvironment(environmentName)).Validate(null, config);

    /// <summary>
    /// Only <see cref="IHostEnvironment.EnvironmentName"/> is read; the rest of the interface exists
    /// to satisfy the compiler.
    /// </summary>
    private sealed class FakeEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Vortex.Supervisor.Tests";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static SupervisorConfig Config(
        string token = GOOD_TOKEN,
        string host = "localhost",
        int port = 5250,
        bool allowInsecure = false
    ) =>
        new()
        {
            Token = token,
            Host = host,
            Port = port,
            AllowInsecureRemoteHttp = allowInsecure,
        };
}
