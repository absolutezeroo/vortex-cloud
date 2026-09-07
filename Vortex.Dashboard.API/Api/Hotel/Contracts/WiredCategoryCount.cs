namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>How many boxes of one kind -- trigger, condition, action -- are placed.</summary>
public sealed record WiredCategoryCount(string Category, int Count);
