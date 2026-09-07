namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>A guild whose forum has been used at all, ranked by posts.</summary>
public sealed record GroupForumRanking(int GroupId, string Name, int ThreadCount, int PostCount);
