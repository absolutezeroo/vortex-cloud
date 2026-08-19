using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Console;

/// <summary>
/// One operator command: the word that invokes it, how to call it, and the dashboard capability a
/// caller needs <i>on top of</i> the console capability itself.
/// </summary>
/// <param name="Name">The lowercase word that selects this command.</param>
/// <param name="Usage">A one-line usage string, shown by <c>help</c>.</param>
/// <param name="Description">What the command does, shown by <c>help</c>.</param>
/// <param name="RequiredCapability">
///     The capability gating this specific command, or <see langword="null"/> when holding the
///     console capability is enough. Reusing the existing capability of whatever the command acts
///     on keeps one grant meaning the same thing whether it is exercised from a page or from here.
/// </param>
/// <param name="Aliases">Alternate words that select the same command.</param>
public sealed record ConsoleCommandDescriptor(
    string Name,
    string Usage,
    string Description,
    string? RequiredCapability = null,
    IReadOnlyList<string>? Aliases = null
);

/// <summary>
/// Runs operator commands and writes their output to a caller-supplied sink.
/// <para>
/// The emulator's stdin loop and the dashboard console are two front-ends onto this one dispatcher,
/// so a command behaves identically whichever way it is reached — and, unlike piping a line into a
/// process's stdin, the caller is known, the command is a typed lookup rather than a blind string,
/// and it can be refused per <see cref="ConsoleCommandDescriptor.RequiredCapability"/>.
/// </para>
/// </summary>
public interface IConsoleCommandDispatcher
{
    /// <summary>Every command this dispatcher understands, in <c>help</c> order.</summary>
    IReadOnlyList<ConsoleCommandDescriptor> Commands { get; }

    /// <summary>
    /// Resolves a command word (or alias) to its descriptor.
    /// </summary>
    /// <returns><see langword="null"/> when no command answers to <paramref name="name"/>.</returns>
    ConsoleCommandDescriptor? Find(string name);

    /// <summary>
    /// Parses and runs <paramref name="input"/>, writing every line the command produces to
    /// <paramref name="write"/>.
    /// </summary>
    /// <returns>
    ///     <see langword="false"/> when the input named no known command — the caller decides
    ///     whether that is worth reporting.
    /// </returns>
    Task<bool> ExecuteAsync(string input, Action<string> write, CancellationToken ct);
}
