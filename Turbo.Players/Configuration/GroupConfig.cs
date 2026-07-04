namespace Turbo.Players.Configuration;

public sealed class GroupConfig
{
    public const string SECTION_NAME = "Turbo:Groups";

    public int MembersPerPage { get; init; } = 14;
    public int CreationCostInCredits { get; init; } = 10;
    public int MaxForumPageSize { get; init; } = 50;
    public int DefaultForumPageSize { get; init; } = 20;
}
