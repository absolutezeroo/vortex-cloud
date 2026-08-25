using FluentAssertions;
using Vortex.Dashboard.API.Operations;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// Which exception messages the dashboard is willing to put on an operator's screen.
/// </summary>
/// <remarks>
/// <para>
/// Every dashboard write runs inside one try/catch, and its <c>InvalidOperationException</c> branch
/// existed for the domain's own refusals — <c>offer_has_products</c>, <c>account_not_found</c> — which
/// are exactly what an operator needs to read. The trouble is that the type is not the domain's
/// alone: EF Core throws it for a tracking conflict or an empty sequence, Orleans for a bad grain
/// reference, and the branch forwarded whatever any of them said, verbatim, to the browser.
/// </para>
/// <para>
/// That is the frozen note's "pas de message SQL/EF/Orleans brut". The shape of the message is the
/// whole discriminator, so it is the whole test: a message that is not a code sends the call to the
/// fault branch instead, which logs the exception and answers a generic code.
/// </para>
/// </remarks>
public sealed class DomainRejectionMessageTests
{
    [Theory]
    [InlineData("offer_has_products")]
    [InlineData("account_not_found")]
    [InlineData("page_has_children")]
    [InlineData("unknown_config_key")]
    [InlineData("invalid_request")]
    [InlineData("kick_rejected")]
    [InlineData("mfa_step_up_required")]
    [InlineData("a")]
    [InlineData("code2")]
    public void ADomainCodeIsSurfaced(string message) =>
        DashboardOperationsService.IsDomainCode(message).Should().BeTrue();

    [Theory]
    // The real ones, quoted. Each of these reached a browser before the guard.
    [InlineData("Sequence contains no elements")]
    [InlineData(
        "The instance of entity type 'CatalogOffer' cannot be tracked because another instance with the same key value for {'Id'} is already being tracked."
    )]
    [InlineData("An error occurred while saving the entity changes. See the inner exception.")]
    [InlineData("Nullable object must have a value.")]
    [InlineData("Response does not indicate success: 500")]
    // Shapes that are close enough to be worth pinning: a capital, a space, a dot, a digit first.
    [InlineData("Offer_has_products")]
    [InlineData("offer has products")]
    [InlineData("offer.has.products")]
    [InlineData("2_much")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsNot(string? message) =>
        DashboardOperationsService.IsDomainCode(message).Should().BeFalse();

    /// <summary>
    /// A length cap on top of the shape. A snake_case message long enough to be prose is prose, and
    /// the codes the domain actually throws are two or three words.
    /// </summary>
    [Fact]
    public void ACodeShapedSentenceIsStillTooLongToBeACode() =>
        DashboardOperationsService.IsDomainCode(new string('a', 65)).Should().BeFalse();
}
