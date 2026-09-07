namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One player in the picker.
/// </summary>
/// <param name="AvatarUrl">Null when the player has no figure, or when the hotel has no avatar
/// renderer configured.</param>
/// <param name="Online">Session state, not a column -- which is why it is applied after the query
/// rather than in it.</param>
public sealed record PlayerDirectoryRow(int Id, string Name, string? AvatarUrl, bool Online);
