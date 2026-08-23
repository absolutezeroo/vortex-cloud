using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Primitives.Authentication;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// What an operator does to their own account, which is currently one thing: turn the second factor
/// on and off.
///
/// <para>
/// These three do not go through <c>MapPost</c>, and that is deliberate. That helper attaches a
/// capability policy and the mandatory audited reason, both of which are about acting on the hotel
/// or on somebody else. Nobody needs a capability to secure their own login, and asking an operator
/// to justify enabling two-factor in a free-text reason box is the kind of friction that ends with
/// the feature switched off. They are still authenticated-only, never anonymous, and every call is
/// recorded by the HTTP access audit like any other route.
/// </para>
///
/// <para>
/// Clearing <em>somebody else's</em> factor is the opposite kind of act and lives where it belongs:
/// <c>ResetAccountMfaAsync</c>, behind <c>OpsStaffManage</c>, with a reason.
/// </para>
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagAccount = "Account";
    private const string ApiAccountMfa = ApiV1 + "/account/mfa";

    public static void MapAccountEndpoints(WebApplication app)
    {
        app.MapPost(
                ApiAccountMfa + "/begin",
                async (HttpContext ctx, IAccountMfaService mfa, CancellationToken ct) =>
                {
                    DashboardPrincipalIds ids = ResolveIds(ctx);

                    if (ids.AccountId is null)
                    {
                        return Unauthenticated();
                    }

                    MfaEnrolment enrolment = await mfa.BeginEnrolmentAsync(ids.AccountId.Value, ct)
                        .ConfigureAwait(false);

                    // Nothing is stored yet: this secret only becomes the account's factor once a
                    // code computed from it comes back to /enable.
                    return Results.Ok(new { secret = enrolment.Secret, uri = enrolment.Uri });
                }
            )
            .RequireAuthorization()
            .WithName("BeginAccountMfaEnrolment")
            .WithSummary("Generate an unstored TOTP secret for the current operator.")
            .WithTags(TagAccount);

        app.MapPost(
                ApiAccountMfa + "/enable",
                async (
                    HttpContext ctx,
                    AccountMfaEnableRequest body,
                    IAccountMfaService mfa,
                    CancellationToken ct
                ) =>
                {
                    DashboardPrincipalIds ids = ResolveIds(ctx);

                    if (ids.AccountId is null)
                    {
                        return Unauthenticated();
                    }

                    if (
                        body is null
                        || string.IsNullOrWhiteSpace(body.Secret)
                        || string.IsNullOrWhiteSpace(body.Code)
                    )
                    {
                        return Results.BadRequest(new { error = "invalid_request" });
                    }

                    bool enabled = await mfa.ConfirmEnrolmentAsync(
                            ids.AccountId.Value,
                            body.Secret,
                            body.Code,
                            ct
                        )
                        .ConfigureAwait(false);

                    return enabled
                        ? Results.Ok(new { enabled = true })
                        : Results.BadRequest(new { error = "invalid_code" });
                }
            )
            .RequireAuthorization()
            .WithName("EnableAccountMfa")
            .WithSummary("Confirm a TOTP secret with a code and store it as the second factor.")
            .WithTags(TagAccount);

        app.MapPost(
                ApiAccountMfa + "/disable",
                async (
                    HttpContext ctx,
                    AccountMfaDisableRequest body,
                    IAccountMfaService mfa,
                    CancellationToken ct
                ) =>
                {
                    DashboardPrincipalIds ids = ResolveIds(ctx);

                    if (ids.AccountId is null)
                    {
                        return Unauthenticated();
                    }

                    if (body is null || string.IsNullOrWhiteSpace(body.Code))
                    {
                        // A current code is required: a stolen session must not be able to take the
                        // factor off the account it stole.
                        return Results.BadRequest(new { error = "invalid_request" });
                    }

                    bool disabled = await mfa.DisableAsync(ids.AccountId.Value, body.Code, ct)
                        .ConfigureAwait(false);

                    return disabled
                        ? Results.Ok(new { enabled = false })
                        : Results.BadRequest(new { error = "invalid_code" });
                }
            )
            .RequireAuthorization()
            .WithName("DisableAccountMfa")
            .WithSummary("Remove the current operator's second factor, proving a current code.")
            .WithTags(TagAccount);
    }

    private static DashboardPrincipalIds ResolveIds(HttpContext ctx) =>
        new(ctx.GetDashboardPrincipal()?.AccountId);

    private static IResult Unauthenticated() =>
        Results.Json(
            new { error = "unauthenticated" },
            statusCode: StatusCodes.Status401Unauthorized
        );

    private readonly record struct DashboardPrincipalIds(int? AccountId);
}

/// <summary>The secret handed out by <c>/begin</c>, plus a code proving an authenticator holds it.</summary>
public sealed record AccountMfaEnableRequest(string? Secret, string? Code);

/// <summary>A current code, proving the caller holds the factor they are switching off.</summary>
public sealed record AccountMfaDisableRequest(string? Code);
