using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Authentication;

/// <summary>What stopped a safety-questions call, or nothing.</summary>
public enum SafetyQuestionsOutcome
{
    Succeeded,
    UnknownAccount,

    /// <summary>The current password was wrong.</summary>
    WrongPassword,

    /// <summary>The account has a second factor and no code came with the request.</summary>
    MfaRequired,

    /// <summary>A code came, and it was not valid.</summary>
    InvalidCode,

    /// <summary>
    /// The pair is not two different questions in 1..<see cref="SafetyQuestions.COUNT" />.
    /// </summary>
    InvalidQuestions,

    /// <summary>An answer was empty once normalised.</summary>
    EmptyAnswer,

    /// <summary>The account has no questions to verify against, or to clear.</summary>
    NotConfigured,

    /// <summary>One or both answers were wrong. Never says which.</summary>
    WrongAnswers,
}

/// <summary>
/// The two questions an account has chosen, or none. Only the NUMBERS travel: their wording is
/// `IDENTITY_SAFETYQUESTION_&lt;n&gt;` in whatever language the reader is using.
/// </summary>
public readonly record struct SafetyQuestionsStatus(bool Configured, int Question1, int Question2)
{
    public static SafetyQuestionsStatus None { get; } = new(false, 0, 0);
}

/// <summary>The catalogue the website offers. There are nine, and habbo.com has the same nine.</summary>
public static class SafetyQuestions
{
    public const int COUNT = 9;

    /// <summary>A pair is valid when both are in range and they are not the same question.</summary>
    public static bool IsValidPair(int question1, int question2) =>
        question1 >= 1
        && question1 <= COUNT
        && question2 >= 1
        && question2 <= COUNT
        && question1 != question2;
}

/// <summary>
/// The account's two security questions: habbo.com's "Protection du compte", which is what arms the
/// safety lock rather than a switch the owner flips by hand.
/// </summary>
/// <remarks>
/// <para>
/// Setting them requires the password (and the second factor, when there is one), because they are
/// a way INTO the account: a thief holding a session who could quietly replace the questions would
/// own the recovery path as well as the session.
/// </para>
/// <para>
/// Verifying them requires neither, by design — that is the whole point of a challenge: the visitor
/// is being asked to prove they are the owner of a session the server is not sure about. It is
/// rate-limited at the endpoint instead, the same policy sign-in uses, because two short answers
/// are guessable at the same speed as a password.
/// </para>
/// </remarks>
public interface IAccountSafetyQuestionsService
{
    Task<SafetyQuestionsStatus> GetStatusAsync(int accountId, CancellationToken ct = default);

    Task<SafetyQuestionsOutcome> SaveAsync(
        int accountId,
        int question1,
        string answer1,
        int question2,
        string answer2,
        string currentPassword,
        string? code,
        CancellationToken ct = default
    );

    Task<SafetyQuestionsOutcome> ClearAsync(
        int accountId,
        string currentPassword,
        string? code,
        CancellationToken ct = default
    );

    /// <summary>
    /// Checks two answers against the stored hashes. Answers are compared after the same
    /// normalisation they were hashed with — trimmed, lower-cased, inner runs of whitespace
    /// collapsed — because a security answer is a phrase a person types from memory months later,
    /// and "Mr Whiskers" is the same answer as "mr  whiskers".
    /// </summary>
    Task<SafetyQuestionsOutcome> VerifyAsync(
        int accountId,
        string answer1,
        string answer2,
        CancellationToken ct = default
    );
}
