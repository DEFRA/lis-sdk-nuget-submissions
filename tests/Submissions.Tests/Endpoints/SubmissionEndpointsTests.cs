// <copyright file="SubmissionEndpointsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Endpoints;

using System.Net;
using System.Text;
using System.Text.Json;
using Defra.Livestock.Sdk.Core.Exceptions;
using Defra.Livestock.Sdk.Core.Extensions;
using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Tests.Support;

/// <summary>
/// Runs the submission endpoints through a minimal host. Host-specific concerns, such as authorization and correlation
/// ids, are covered by each host's own tests.
/// </summary>
public sealed class SubmissionEndpointsTests : IAsyncLifetime
{
    private const string Cph = "12/345/6789";
    private const string StartUrl = "/submission/12/345/6789/start";
    private const string ListUrl = "/submission/12/345/6789";
    private const string Animal = "UK123123123";
    private const string DateOfBirth = """[{"key":"dob","value":"2026-11-01","type":"DATE"}]""";

    private SubmissionTestHost host = null!;

    private HttpClient Client => host.Client;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        host = await SubmissionTestHost.StartAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await host.DisposeAsync();
    }

    /// <summary>
    /// Verifies: Start should create a draft with no animals.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task StartShouldCreateADraftWithNoAnimals()
    {
        using var body = await GetJsonAsync(StartUrl);

        body.RootElement.GetProperty("submission-id").GetString().ShouldNotBeNullOrWhiteSpace();
        body.RootElement.GetProperty("animals").GetArrayLength().ShouldBe(0);
        body.RootElement.TryGetProperty("submitted-date", out _).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies: Start should use the core display hash of the guid as the id.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task StartShouldUseTheCoreDisplayHashOfTheGuidAsTheId()
    {
        var id = await StartAsync();

        var stored = host.Repository.Stored.ShouldHaveSingleItem();
        stored.HashId.ShouldBe(id);
        stored.HashId.ShouldBe(Guid.Parse(stored.Id).CreateDisplayHash());
    }

    /// <summary>Without a key, start resumes the latest draft and leaves other drafts intact.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task StartWithoutKeyShouldResumeLatestDraft()
    {
        var first = await StartAsync();
        var latest = new SubmissionDocument
        {
            Id = Guid.NewGuid().ToString(),
            HashId = "ABC-DEF-GHJ",
            Cph = Cph,
            CreatedDate = DateTime.UtcNow.AddMinutes(1),
            Status = SubmissionStatus.Draft,
        };
        await host.Repository.TryInsertDraftAsync(latest, TestContext.Current.CancellationToken);

        (await StartAsync()).ShouldBe(latest.HashId);
        host.Repository.Stored.Count.ShouldBe(2);
        host.Repository.Stored.ShouldContain(submission => submission.HashId == first);
    }

    /// <summary>A subsequent start reuses the draft created by the first call.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task StartWithoutKeyShouldReuseExistingDraft()
    {
        var first = await StartAsync();

        (await StartAsync()).ShouldBe(first);
        host.Repository.Stored.ShouldHaveSingleItem();
    }

    /// <summary>A newer submitted document or another holding's draft is not resumed.</summary>
    /// <param name="status">The newer submission's status.</param>
    /// <param name="cph">The newer submission's holding.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(SubmissionStatus.Submitted, Cph)]
    [InlineData(SubmissionStatus.Draft, "98/765/4321")]
    public async Task StartWithoutKeyShouldIgnoreIneligibleSubmissions(string status, string cph)
    {
        var first = await StartAsync();
        await host.Repository.TryInsertDraftAsync(
            new SubmissionDocument
            {
                Id = Guid.NewGuid().ToString(),
                HashId = "ABC-DEF-GHJ",
                Cph = cph,
                CreatedDate = DateTime.UtcNow.AddMinutes(1),
                Status = status,
            },
            TestContext.Current.CancellationToken);

        (await StartAsync()).ShouldBe(first);
        host.Repository.Stored.Count.ShouldBe(2);
    }

    /// <summary>A submitted document does not prevent creating a draft when no draft exists.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task StartWithoutKeyShouldCreateDraftWhenOnlySubmittedDocumentsExist()
    {
        await host.Repository.TryInsertDraftAsync(
            new SubmissionDocument
            {
                Id = Guid.NewGuid().ToString(),
                HashId = "ABC-DEF-GHJ",
                Cph = Cph,
                CreatedDate = DateTime.UtcNow,
                Status = SubmissionStatus.Submitted,
            },
            TestContext.Current.CancellationToken);

        var id = await StartAsync();

        id.ShouldNotBe("ABC-DEF-GHJ");
        host.Repository.Stored.Count.ShouldBe(2);
        host.Repository.Stored.ShouldContain(submission => submission.HashId == id && submission.Status == SubmissionStatus.Draft);
    }

    /// <summary>A key resumes the selected draft, preserving its animals even when another draft exists.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task StartWithKeyShouldResumeSelectedDraft()
    {
        var first = await StartAsync();
        using var saved = await PostAsync($"{ListUrl}/{first}/{Animal}", AnimalJson("complete", DateOfBirth));
        saved.StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = "ABC-DEF-GHJ";
        await host.Repository.TryInsertDraftAsync(
            new SubmissionDocument
            {
                Id = Guid.NewGuid().ToString(),
                HashId = second,
                Cph = Cph,
                CreatedDate = DateTime.UtcNow.AddMinutes(1),
                Status = SubmissionStatus.Draft,
            },
            TestContext.Current.CancellationToken);

        using var resumed = await GetJsonAsync($"{StartUrl}?key={first.ToLowerInvariant()}");

        resumed.RootElement.GetProperty("submission-id").GetString().ShouldBe(first);
        resumed.RootElement.GetProperty("animals").GetArrayLength().ShouldBe(1);
        host.Repository.Stored.Count.ShouldBe(2);
        host.Repository.Stored.ShouldContain(submission => submission.HashId == second && submission.Animals.Count == 0);
    }

    /// <summary>Unknown or empty keys do not silently create drafts.</summary>
    /// <param name="key">The supplied key.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData("missing")]
    [InlineData("")]
    public async Task StartWithUnknownKeyShouldReturn404(string key)
    {
        using var response = await Client.GetAsync($"{StartUrl}?key={key}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        host.Repository.Stored.ShouldBeEmpty();
    }

    /// <summary>A key cannot resume another holding's submission.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task StartWithAnotherCphKeyShouldReturn404()
    {
        var id = await StartAsync();

        using var response = await Client.GetAsync($"/submission/98/765/4321/start?key={id}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        host.Repository.Stored.Count.ShouldBe(1);
    }

    /// <summary>A submitted draft cannot be resumed.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task StartWithSubmittedKeyShouldReturn409()
    {
        var id = await StartAsync();
        var submission = host.Repository.Stored.ShouldHaveSingleItem();
        submission.Status = SubmissionStatus.Submitted;
        await host.Repository.TryInsertDraftAsync(submission, TestContext.Current.CancellationToken);

        using var response = await Client.GetAsync($"{StartUrl}?key={id}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        host.Repository.Stored.ShouldHaveSingleItem().Status.ShouldBe(SubmissionStatus.Submitted);
    }

    /// <summary>
    /// Verifies: Start should try again when the hash id clashes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task StartShouldTryAgainWhenTheHashIdClashes()
    {
        host.Repository.HashIdClashes = 2;

        var id = await StartAsync();

        host.Repository.Stored.ShouldHaveSingleItem().HashId.ShouldBe(id);
    }

    /// <summary>
    /// Verifies: Start should return 409 when the hash id keeps clashing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task StartShouldReturn409WhenTheHashIdKeepsClashing()
    {
        host.Repository.HashIdClashes = 3;

        using var response = await Client.GetAsync(StartUrl, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Verifies: Start should return 400 for an invalid cph.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task StartShouldReturn400ForAnInvalidCph()
    {
        using var response = await Client.GetAsync("/submission/1/345/6789/start", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies: An animal should be saved read back and deleted.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task AnAnimalShouldBeSavedReadBackAndDeleted()
    {
        var id = await StartAsync();

        using (var saved = await PostAsync($"{ListUrl}/{id}/{Animal}", AnimalJson("complete", DateOfBirth)))
        {
            saved.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using (var animal = await GetJsonAsync($"{ListUrl}/{id}/{Animal}"))
        {
            animal.RootElement.GetProperty("animal-id").GetString().ShouldBe(Animal);
            animal.RootElement.GetProperty("status").GetString().ShouldBe("complete");
            var item = animal.RootElement.GetProperty("data").EnumerateArray().ShouldHaveSingleItem();
            item.GetProperty("value").GetString().ShouldBe("2026-11-01");
        }

        using (var deleted = await Client.DeleteAsync($"{ListUrl}/{id}/{Animal}", TestContext.Current.CancellationToken))
        {
            deleted.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        host.Repository.Stored.ShouldHaveSingleItem().Animals.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies: Endpoints should accept the id in lower case.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task EndpointsShouldAcceptTheIdInLowerCase()
    {
        var id = await StartAsync();

        using var response = await Client.GetAsync($"{ListUrl}/{id.ToLowerInvariant()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies: A submission for another cph should not be found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ASubmissionForAnotherCphShouldNotBeFound()
    {
        var id = await StartAsync();

        using var response = await Client.GetAsync($"/submission/98/765/4321/{id}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies: Saving should return 400 for an unsupported type.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task SavingShouldReturn400ForAnUnsupportedType()
    {
        var id = await StartAsync();

        using var response = await PostAsync(
            $"{ListUrl}/{id}/{Animal}",
            AnimalJson("draft", """[{"key":"dob","value":"x","type":"COLOUR"}]"""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies: Saving should return 409 when the draft changed meanwhile.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task SavingShouldReturn409WhenTheDraftChangedMeanwhile()
    {
        var id = await StartAsync();
        host.Repository.RejectNextReplace = true;

        using var response = await PostAsync($"{ListUrl}/{id}/{Animal}", AnimalJson("draft", DateOfBirth));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Verifies: Submit should delete the submission after confirmed transfer.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task SubmitShouldDeleteTheSubmissionAfterConfirmedTransfer()
    {
        var id = await StartAsync();
        (await PostAsync($"{ListUrl}/{id}/{Animal}", AnimalJson("complete", DateOfBirth))).Dispose();

        using (var submitted = await PostAsync($"{ListUrl}/{id}"))
        {
            submitted.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        host.Transfer.TransferredIds.ShouldBe([id]);
        host.Repository.Stored.ShouldBeEmpty();
        using var response = await Client.GetAsync($"{ListUrl}/{id}", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies: A submitted draft should no longer change.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ASubmittedDraftShouldNoLongerChange()
    {
        var id = await StartAsync();
        (await PostAsync($"{ListUrl}/{id}/{Animal}", AnimalJson("complete", DateOfBirth))).Dispose();
        (await PostAsync($"{ListUrl}/{id}")).Dispose();

        using var response = await PostAsync($"{ListUrl}/{id}/{Animal}", AnimalJson("draft", DateOfBirth));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies: Submit should return 400 when there are no animals.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task SubmitShouldReturn400WhenThereAreNoAnimals()
    {
        var id = await StartAsync();

        using var response = await PostAsync($"{ListUrl}/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        host.Transfer.TransferredIds.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies: Submit should leave a draft when the transfer is unavailable.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task SubmitShouldLeaveADraftWhenTheTransferIsUnavailable()
    {
        var id = await StartAsync();
        (await PostAsync($"{ListUrl}/{id}/{Animal}", AnimalJson("complete", DateOfBirth))).Dispose();
        host.Transfer.FailWith = new ServiceUnavailableException("Downstream is unavailable.");

        using var response = await PostAsync($"{ListUrl}/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        host.Repository.Stored.ShouldHaveSingleItem().Status.ShouldBe(SubmissionStatus.Draft);
    }

    /// <summary>
    /// Verifies: List should return the cphs submissions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ListShouldReturnTheCphsSubmissions()
    {
        var id = await StartAsync();

        using var body = await GetJsonAsync(ListUrl);

        body.RootElement.GetProperty("submissions").EnumerateArray().ShouldHaveSingleItem()
            .GetProperty("submission-id").GetString().ShouldBe(id);
        host.Repository.Stored.ShouldHaveSingleItem().Cph.ShouldBe(Cph);
    }

    /// <summary>
    /// Verifies: Endpoints should use the route prefix given.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task EndpointsShouldUseTheRoutePrefixGiven()
    {
        await using var prefixed = await SubmissionTestHost.StartAsync("/death-submission/");

        using var response = await prefixed.Client.GetAsync("/death-submission/12/345/6789/start", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static string AnimalJson(string status, string data) => $$"""{"status":"{{status}}","data":{{data}}}""";

    private async Task<string> StartAsync()
    {
        using var body = await GetJsonAsync(StartUrl);
        return body.RootElement.GetProperty("submission-id").GetString()!;
    }

    private async Task<JsonDocument> GetJsonAsync(string url)
    {
        using var response = await Client.GetAsync(url, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private async Task<HttpResponseMessage> PostAsync(string url, string? json = null)
    {
        using var content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.PostAsync(url, content, TestContext.Current.CancellationToken);
    }
}
