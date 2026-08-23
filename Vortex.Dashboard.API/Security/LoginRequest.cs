namespace Vortex.Dashboard.API.Security;

/// <summary>
/// Credentials posted to <c>POST /api/login</c> to start a dashboard session. <paramref name="Code" />
/// is the second factor, absent on the first attempt: an account that has one answers
/// <c>mfa_required</c>, and the client resubmits the same credentials with the code. Keeping it on
/// the one request avoids a challenge token with its own lifetime, expiry and replay window.
/// </summary>
public sealed record LoginRequest(string? Email, string? Password, string? Code = null);
