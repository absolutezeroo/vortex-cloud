using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Vortex.Dashboard.API.Security;
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
            .RequireRateLimiting(MfaRateLimitPolicy)
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
            .RequireRateLimiting(MfaRateLimitPolicy)
            .WithName("DisableAccountMfa")
            .WithSummary("Remove the current operator's second factor, proving a current code.")
            .WithTags(TagAccount);

        app.MapPost(
                ApiAccountMfa + "/step-up",
                async (
                    HttpContext ctx,
                    AccountMfaStepUpRequest body,
                    IAccountMfaService mfa,
                    DashboardSessionStore sessions,
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
                        return Results.BadRequest(new { error = "invalid_request" });
                    }

                    if (
                        !await mfa.VerifyAsync(ids.AccountId.Value, body.Code, ct)
                            .ConfigureAwait(false)
                    )
                    {
                        return Results.BadRequest(new { error = "invalid_code" });
                    }

                    // Stamped on the session, not the account: a second window opened with a stolen
                    // cookie must not inherit a step-up the real operator did in theirs. A false here
                    // means the cookie names no live session, which the authentication layer would
                    // have caught -- unless the session expired between the two, and then saying so
                    // is better than reporting a step-up nothing is holding.
                    string? sessionId = ctx.Request.Cookies[
                        DashboardAuthenticationHandler.SessionCookieName
                    ];

                    return sessions.MarkSteppedUp(sessionId)
                        ? Results.Ok(new { steppedUp = true })
                        : Unauthenticated();
                }
            )
            .RequireAuthorization()
            .RequireRateLimiting(MfaRateLimitPolicy)
            .WithName("StepUpAccountMfa")
            .WithSummary(
                "Prove a current second-factor code, unlocking critical operations for a while."
            )
            .WithTags(TagAccount);

        app.MapPost(
                ApiV1 + "/account/password",
                async (
                    HttpContext ctx,
                    AccountPasswordChangeRequest body,
                    IAccountPasswordService passwords,
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
                        || string.IsNullOrEmpty(body.CurrentPassword)
                        || string.IsNullOrEmpty(body.NewPassword)
                    )
                    {
                        return Results.BadRequest(new { error = "invalid_request" });
                    }

                    PasswordChangeResult result = await passwords
                        .ChangeAsync(
                            ids.AccountId.Value,
                            body.CurrentPassword,
                            body.NewPassword,
                            body.Code,
                            ct
                        )
                        .ConfigureAwait(false);

                    if (!result.Succeeded)
                    {
                        return Results.BadRequest(new { error = DescribeFailure(result.Outcome) });
                    }

                    // The change revoked every session of the account, this one included -- being
                    // signed out everywhere is what changing a password is for. Drop the cookie so
                    // the browser stops presenting a token that no longer resolves.
                    ctx.Response.Cookies.Delete(
                        DashboardAuthenticationHandler.SessionCookieName,
                        new CookieOptions { Path = "/" }
                    );

                    return Results.Ok(
                        new { changed = true, sessionsRevoked = result.SessionsRevoked }
                    );
                }
            )
            .RequireAuthorization()
            .WithName("ChangeAccountPassword")
            .WithSummary("Change the current operator's password and sign them out everywhere.")
            .WithTags(TagAccount);
    }

    /// <summary>
    /// One error code per refusal. "Too short" and "wrong password" have to be distinguishable or the
    /// operator cannot tell a typo from a rule; the second factor's two are already familiar from the
    /// login screen.
    /// </summary>
    private static string DescribeFailure(PasswordChangeOutcome outcome) =>
        outcome switch
        {
            PasswordChangeOutcome.MfaRequired => "mfa_required",
            PasswordChangeOutcome.InvalidCode => "invalid_code",
            PasswordChangeOutcome.TooShort => "password_too_short",
            PasswordChangeOutcome.UnknownAccount => "unknown_account",
            _ => "wrong_password",
        };

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

/// <summary>
/// A current code, proving the operator is still the operator before a critical operation runs.
/// Carries nothing else on purpose: what the step-up is <em>for</em> is not the browser's to say.
/// </summary>
public sealed record AccountMfaStepUpRequest(string? Code);

/// <summary>
/// The current password, the new one, and a code when the account has a second factor. The current
/// password is required even though the caller already holds a session: a stolen cookie must not be
/// enough to take the account.
/// </summary>
public sealed record AccountPasswordChangeRequest(
    string? CurrentPassword,
    string? NewPassword,
    string? Code
);
