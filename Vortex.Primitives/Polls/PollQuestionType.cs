namespace Vortex.Primitives.Polls;

/// <summary>
/// Input widget the client builds for a poll question. The values are the client's own, read
/// straight off the wire: <c>PollContentDialog</c> switches on <c>questionType - 1</c> and maps
/// 0..3 to radio / checkbox / single-line text / text area. Anything else makes the dialog skip
/// the question, so <see cref="Rating"/> and <see cref="Binary"/> — declared in the client's poll
/// enum but not handled by the content dialog — must not be used for survey questions.
/// </summary>
public enum PollQuestionType
{
    /// <summary>One choice out of a radio-button list. Choices are sent on the wire.</summary>
    SingleChoice = 1,

    /// <summary>Any number of choices out of a checkbox list. Choices are sent on the wire.</summary>
    MultipleChoice = 2,

    /// <summary>Free text, single line. No choices are sent.</summary>
    TextLine = 3,

    /// <summary>Free text, multi-line. No choices are sent.</summary>
    TextArea = 4,

    /// <summary>Word-quiz only (the content dialog skips it).</summary>
    Rating = 5,

    /// <summary>Word-quiz only (the content dialog skips it).</summary>
    Binary = 6,
}
