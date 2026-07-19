namespace Vortex.WebApi.Http;

/// <summary>
/// Strongly typed request bodies for the web API POST endpoints. Each record exposes a
/// <c>IsValid</c> predicate so the endpoint can reject malformed payloads with a clean 400 before
/// any service work happens, replacing the hand-rolled null/whitespace checks of the old listener.
/// </summary>
public sealed record LoginRequest(string? Email, string? Password)
{
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
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

public sealed record CreateAvatarRequest(string? Name, string? Figure, string? Gender)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
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
    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && PlayerId > 0;
}

public sealed record SaveFigureRequest(string? FigureString, string? Gender, int PlayerId)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(FigureString) && PlayerId > 0;
}
