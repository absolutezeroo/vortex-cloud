namespace Vortex.Primitives.Moderation;

/// <summary>One chat line the reporter selected as evidence, from their local chat buffer.</summary>
public readonly record struct CfhEvidenceLine(int UserId, string Text);
