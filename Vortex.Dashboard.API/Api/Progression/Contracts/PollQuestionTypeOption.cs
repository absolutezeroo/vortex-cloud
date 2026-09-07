namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One question type.
/// </summary>
/// <param name="Supported">
/// Whether the client's survey dialog actually renders it. Rating and Binary exist in the client's
/// enum but its content dialog skips them, so a survey built on those would show the player
/// nothing — which an operator can only know if the picker says so.
/// </param>
/// <param name="TakesChoices">Whether the editor should offer a choice list for it.</param>
public sealed record PollQuestionTypeOption(int Id, string Name, bool Supported, bool TakesChoices);
