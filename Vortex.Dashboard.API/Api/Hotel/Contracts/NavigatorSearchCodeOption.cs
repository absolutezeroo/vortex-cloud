namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One search code the client knows.
/// </summary>
/// <param name="TopLevel">Whether it is one of the codes the client asks for as a tab of its own.</param>
public sealed record NavigatorSearchCodeOption(
    string Code,
    int QueryType,
    string QueryTypeLabel,
    bool TopLevel
);
