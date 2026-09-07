using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Api.Progression;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Database.Context;
using Vortex.Database.Entities.Polls;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Polls;
using Xunit;

namespace Vortex.Dashboard.Tests;

/// <summary>
/// The JSON the polls read surface actually puts on the wire, field by field.
/// </summary>
/// <remarks>
/// <para>
/// Written before giving those responses explicit types. The reads return <c>object</c> holding
/// anonymous types, so the contract the front end depends on exists only as the shape those
/// literals happen to produce — nothing declares it and nothing checks it. This pins it, so
/// replacing the literals with records can be shown to change no name and no nesting.
/// </para>
/// <para>
/// It asserts on property names rather than values on purpose. A renamed field breaks
/// <c>PollsPage.svelte</c> at runtime with no build error on either side, and that is the failure
/// this has to catch; whether a count is 3 or 4 is the business of the tests that already cover it.
/// </para>
/// <para>
/// Serialised with <see cref="JsonSerializerDefaults.Web" />, which is what minimal APIs use and why
/// a C# <c>RootQuestionCount</c> and an anonymous <c>rootQuestionCount</c> reach the browser
/// identically.
/// </para>
/// </remarks>
public sealed class PollContractTests
{
    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task The_list_names_every_field_the_page_reads()
    {
        JsonElement list = await Serialize(
            Reads(await SeededAsync()).PollsAsync(new NameValueCollection(), CancellationToken.None)
        );

        Names(list).Should().BeEquivalentTo(["count", "items"]);

        Names(list.GetProperty("items")[0])
            .Should()
            .BeEquivalentTo([
                "id",
                "code",
                "pollType",
                "headline",
                "summary",
                "startMessage",
                "endMessage",
                "npsPoll",
                "enabled",
                "offerOnRoomEntry",
                "roomId",
                "roomName",
                "roomMissing",
                "sortOrder",
                "rootQuestionCount",
                "followUpCount",
                "offeredCount",
                "startedCount",
                "completedCount",
                "rejectedCount",
                "completionRate",
                "offerable",
            ]);
    }

    [Fact]
    public async Task The_detail_names_every_field_the_editor_reads()
    {
        JsonElement detail = await Serialize(
            Reads(await SeededAsync()).PollDetailAsync(1, CancellationToken.None)
        );

        Names(detail)
            .Should()
            .BeEquivalentTo([
                "id",
                "code",
                "pollType",
                "headline",
                "summary",
                "startMessage",
                "endMessage",
                "npsPoll",
                "enabled",
                "offerOnRoomEntry",
                "roomId",
                "roomName",
                "sortOrder",
                "questions",
            ]);

        JsonElement question = detail.GetProperty("questions")[0];

        Names(question)
            .Should()
            .BeEquivalentTo([
                "id",
                "sortOrder",
                "questionType",
                "questionTypeName",
                "questionText",
                "questionCategory",
                "questionAnswerType",
                "answerCount",
                "choices",
                "children",
            ]);

        Names(question.GetProperty("choices")[0])
            .Should()
            .BeEquivalentTo(["id", "value", "choiceText", "choiceType", "sortOrder"]);

        // The tree is one level deep in the client, but the shape is recursive and the editor walks
        // it as such.
        Names(question.GetProperty("children")[0]).Should().Contain("children");
    }

    [Fact]
    public async Task The_results_name_every_field_the_report_reads()
    {
        JsonElement results = await Serialize(
            Reads(await SeededAsync()).PollResultsAsync(1, CancellationToken.None)
        );

        Names(results)
            .Should()
            .BeEquivalentTo(["id", "code", "headline", "npsPoll", "funnel", "questions"]);

        Names(results.GetProperty("funnel"))
            .Should()
            .BeEquivalentTo([
                "offered",
                "pending",
                "started",
                "completed",
                "rejected",
                "completionRate",
                "rejectionRate",
            ]);

        JsonElement question = results
            .GetProperty("questions")
            .EnumerateArray()
            .First(q => q.GetProperty("tally").GetArrayLength() > 0);

        Names(question)
            .Should()
            .BeEquivalentTo([
                "id",
                "questionText",
                "questionType",
                "questionTypeName",
                "isFollowUp",
                "parentQuestionId",
                "questionCategory",
                "respondents",
                "answerCount",
                "tally",
                "freeText",
                "freeTextTruncated",
            ]);

        Names(question.GetProperty("tally")[0])
            .Should()
            .BeEquivalentTo(["value", "text", "choiceType", "count", "share", "retired"]);
    }

    [Fact]
    public void The_question_type_options_name_every_field_the_picker_reads()
    {
        JsonElement options = JsonSerializer.SerializeToElement(
            Reads(NewOptions()).PollQuestionTypeOptions(),
            Wire
        );

        Names(options).Should().BeEquivalentTo(["count", "items"]);
        Names(options.GetProperty("items")[0])
            .Should()
            .BeEquivalentTo(["id", "name", "supported", "takesChoices"]);
    }

    private static IEnumerable<string> Names(JsonElement element) =>
        element.EnumerateObject().Select(property => property.Name);

    private static async Task<JsonElement> Serialize<T>(Task<T> read) =>
        JsonSerializer.SerializeToElement(await read, Wire);

    /// <summary>
    /// One poll with a follow-up question, a choice question and a free-text question, which is the
    /// smallest seed that reaches every branch of the three reads.
    /// </summary>
    private static async Task<DbContextOptions<VortexDbContext>> SeededAsync()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await using VortexDbContext db = new(options);

        db.Polls.Add(
            new PollEntity
            {
                Id = 1,
                Code = "survey",
                Headline = "How is the hotel?",
                Summary = "A few questions",
                Enabled = true,
            }
        );
        db.PollQuestions.AddRange(
            new PollQuestionEntity
            {
                Id = 10,
                PollEntityId = 1,
                QuestionType = PollQuestionType.SingleChoice,
                QuestionText = "Pick one",
            },
            new PollQuestionEntity
            {
                Id = 11,
                PollEntityId = 1,
                ParentQuestionEntityId = 10,
                QuestionType = PollQuestionType.TextArea,
                QuestionText = "Tell us more",
            }
        );
        db.PollQuestionChoices.Add(
            new PollQuestionChoiceEntity
            {
                Id = 20,
                QuestionEntityId = 10,
                Value = "yes",
                ChoiceText = "Yes",
            }
        );
        db.PlayerPollAnswers.Add(
            new PlayerPollAnswerEntity
            {
                Id = 30,
                PollEntityId = 1,
                QuestionEntityId = 10,
                PlayerEntityId = 40,
                Answer = "yes",
                AnsweredAt = DateTime.UtcNow,
            }
        );
        db.PlayerPolls.Add(
            new PlayerPollEntity
            {
                Id = 50,
                PollEntityId = 1,
                PlayerEntityId = 40,
                State = PollParticipationState.Completed,
            }
        );

        await db.SaveChangesAsync();
        return options;
    }

    private static PollReads Reads(DbContextOptions<VortexDbContext> options) =>
        new(
            new TestContextFactory(options),
            new DashboardAssetUrls(Options.Create(new ObservabilityConfig()))
        );

    private static DbContextOptions<VortexDbContext> NewOptions() =>
        new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"poll-contract-{Guid.NewGuid():N}")
            .Options;

    private sealed class TestContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
