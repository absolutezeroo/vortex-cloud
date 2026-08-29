using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;

namespace Vortex.WebApi.Http;

/// <summary>
/// Strongly typed request bodies for the web API POST endpoints. Each record exposes a
/// <c>IsValid</c> predicate so the endpoint can reject malformed payloads with a clean 400 before
/// any service work happens, replacing the hand-rolled null/whitespace checks of the old listener.
/// </summary>
public sealed record LoginRequest(string? Email, string? Password, string? Code = null)
{
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}

/// <summary>
/// A password change for the signed-in account. The current password is required even though the
/// caller holds a session cookie: a stolen cookie must not be enough to take the account.
/// </summary>
public sealed record ChangePasswordRequest(
    string? CurrentPassword,
    string? NewPassword,
    string? Code
)
{
    public bool IsValid =>
        !string.IsNullOrEmpty(CurrentPassword) && !string.IsNullOrEmpty(NewPassword);
}

public sealed record RegisterRequest(string? Email, string? Password, string? PasswordRepeated)
{
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(Password)
        // The confirmation field is optional (some clients omit it), but when supplied it
        // must match the password.
        && (PasswordRepeated is null || Password == PasswordRepeated);
}

/// <remarks>
/// <see cref="Name"/> is optional: the client posts this route with an empty name right after
/// registering, and <c>WebApiPlayerService.CreateAvatarAsync</c> assigns a placeholder that the
/// client's onboarding step replaces. Everything else about the request is unchanged.
/// </remarks>
public sealed record CreateAvatarRequest(string? Name, string? Figure, string? Gender)
{
    // Blank stays valid — that is the registration path, which wants the placeholder. A name that IS
    // supplied has to be one the player could have picked in-game; WebApiPlayerService refuses it
    // again at the sink, this is only so the caller gets a 400 rather than a 409 about a name clash
    // that is not what went wrong.
    public bool IsValid => string.IsNullOrWhiteSpace(Name) || NameShape.IsWellFormed(Name);
}

public sealed record SelectAvatarRequest(string? UniqueId)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(UniqueId);
}

public sealed record NameRequest(string? Name)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
}

public sealed record NameSelectRequest(string? Name, int PlayerId)
{
    public bool IsValid => NameShape.IsWellFormed(Name) && PlayerId > 0;
}

/// <summary>
/// The player-name shape rule, borrowed from the one place that owns it so the HTTP routes and the
/// in-game rename cannot drift apart. Uniqueness is still storage's call, not this.
/// </summary>
internal static class NameShape
{
    public static bool IsWellFormed(string? name) =>
        NameChangePolicy.Validate(
            name,
            NameChangePolicy.DEFAULT_MIN_LENGTH,
            NameChangePolicy.DEFAULT_MAX_LENGTH
        ) == NameChangeResultCode.Ok;
}

public sealed record SaveFigureRequest(string? FigureString, string? Gender, int PlayerId)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(FigureString) && PlayerId > 0;
}
