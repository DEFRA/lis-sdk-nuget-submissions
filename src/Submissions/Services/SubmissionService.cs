// <copyright file="SubmissionService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Services;

using Defra.Livestock.Sdk.Core.Exceptions;
using Defra.Livestock.Sdk.Core.Extensions;
using Defra.Livestock.Sdk.Submissions.Exceptions;
using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Routing;
using Defra.Livestock.Sdk.Submissions.Storage;
using Microsoft.Extensions.Logging;

/// <summary>
/// The submission process, keeping drafts in an <see cref="ISubmissionRepository"/> and submitting them through an
/// <see cref="ISubmissionTransfer"/>.
/// </summary>
/// <param name="repository">Stores the submissions.</param>
/// <param name="transfer">Transfers a submitted draft downstream.</param>
/// <param name="timeProvider">Provides the created and submitted dates.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class SubmissionService(
    ISubmissionRepository repository,
    ISubmissionTransfer transfer,
    TimeProvider timeProvider,
    ILogger<SubmissionService> logger)
    : ISubmissionService
{
    private const int MaxCreateDraftAttempts = 3;

    public Task<SubmissionResponse> StartAsync(
        string cph,
        CancellationToken cancellationToken = default)
    {
        return StartAsync(cph, null, cancellationToken);
    }

    public async Task<SubmissionResponse> StartAsync(
        string cph,
        string? key,
        CancellationToken cancellationToken)
    {
        var normalisedCph = Cph.Normalise(cph);
        var draft = key is null
            ? await repository.FindDraftAsync(normalisedCph, cancellationToken)
                ?? await CreateDraftAsync(normalisedCph, cancellationToken)
            : await GetRequiredDraftAsync(normalisedCph, key, cancellationToken);

        return draft.ToSubmissionResponse();
    }

    public async Task<SubmissionResponse> GetSubmissionAsync(
        string cph,
        string submissionId,
        CancellationToken cancellationToken = default)
    {
        var submission = await GetRequiredAsync(cph, submissionId, cancellationToken);
        return submission.ToSubmissionResponse();
    }

    public async Task<AnimalResponse> GetAnimalAsync(
        string cph,
        string submissionId,
        string animalId,
        CancellationToken cancellationToken = default)
    {
        var submission = await GetRequiredAsync(cph, submissionId, cancellationToken);
        var normalisedAnimalId = animalId.Trim();

        var stored = submission
            .Animals
            .FirstOrDefault(animal => animal.AnimalId == normalisedAnimalId)
            ?? throw new AnimalNotFoundException(normalisedAnimalId, submissionId);

        return stored.ToAnimalResponse();
    }

    public async Task UpsertAnimalAsync(
        string cph,
        string submissionId,
        string animalId,
        AnimalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var submission = await GetRequiredDraftAsync(cph, submissionId, cancellationToken);
        var normalisedAnimalId = animalId.Trim();

        if (string.IsNullOrWhiteSpace(normalisedAnimalId))
        {
            throw new AnimalIdRequiredException(nameof(animalId));
        }

        var animal = request.ToSubmissionAnimalDocument(normalisedAnimalId);
        submission.Animals.RemoveAll(candidate => candidate.AnimalId == normalisedAnimalId);
        submission.Animals.Add(animal);
        await SaveAsync(submission, cancellationToken);
    }

    public async Task DeleteAnimalAsync(
        string cph,
        string submissionId,
        string animalId,
        CancellationToken cancellationToken = default)
    {
        var submission = await GetRequiredDraftAsync(cph, submissionId, cancellationToken);
        var normalisedAnimalId = animalId.Trim();

        if (submission.Animals.RemoveAll(animal => animal.AnimalId == normalisedAnimalId) == 0)
        {
            throw new AnimalNotFoundException(normalisedAnimalId, submissionId);
        }

        await SaveAsync(submission, cancellationToken);

        LogDeletedAnimal(submission.HashId, submission.Animals.Count);
    }

    public async Task SubmitAsync(
        string cph,
        string submissionId,
        CancellationToken cancellationToken = default)
    {
        var submission = await GetRequiredDraftAsync(cph, submissionId, cancellationToken);

        if (submission.Animals.Count == 0)
        {
            throw new EmptySubmissionException(submissionId);
        }

        try
        {
            await transfer.TransferAsync(submission.ToSubmissionTransferRequest(), cancellationToken);
        }
        catch (ServiceUnavailableException ex)
        {
            // The framework leaves the retry loop to be decided. For now it stays a draft and the caller can try again.
            LogSubmissionTransferFailed(submissionId, ex);
            throw;
        }

        submission.Status = SubmissionStatus.Submitted;
        submission.SubmittedDate = timeProvider.GetUtcNow().UtcDateTime;
        await SaveAsync(submission, cancellationToken);

        if (!await repository.DeleteSubmittedAsync(submission.Id, submission.Version, cancellationToken))
        {
            throw new SubmissionChangedException(submission.HashId);
        }

        LogSubmittedSubmission(submissionId, submission.Animals.Count);
    }

    public async Task<SubmissionListResponse> ListAsync(string? cph, CancellationToken cancellationToken = default)
    {
        var submissions = await repository
            .ListAsync(
                Cph.Normalise(cph),
                cancellationToken);

        return new SubmissionListResponse
        {
            Submissions =
            [
                .. submissions
                    .Select(submission => submission.ToSubmissionResponse()),
            ],
        };
    }

    private async Task<SubmissionDocument> CreateDraftAsync(string cph, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCreateDraftAttempts; attempt++)
        {
            var guid = Guid.NewGuid();
            var created = new SubmissionDocument
            {
                Id = guid.ToString(),
                HashId = guid.CreateDisplayHash(),
                Cph = cph,
                CreatedDate = timeProvider.GetUtcNow().UtcDateTime,
                Status = SubmissionStatus.Draft,
            };

            if (await repository.TryInsertDraftAsync(created, cancellationToken))
            {
                LogCreatedDraftSubmission(created.HashId, cph);
                return created;
            }

            // A hash id clash is retried with a new GUID.
        }

        throw new DraftSubmissionCreationException(cph);
    }

    private async Task<SubmissionDocument> GetRequiredAsync(
        string cph,
        string submissionId,
        CancellationToken cancellationToken)
    {
        var result = await repository.GetByHashIdAsync(
            SubmissionHashId.Normalise(submissionId),
            cancellationToken);
        if (result is null)
        {
            throw new SubmissionNotFoundException(submissionId);
        }

        if (result.Cph != cph)
        {
            LogRequestedSubmissionDoesNotBelongToCph(submissionId, cph);
            throw new SubmissionNotFoundException(submissionId);
        }

        return result;
    }

    private async Task<SubmissionDocument> GetRequiredDraftAsync(
        string cph,
        string submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await GetRequiredAsync(cph, submissionId, cancellationToken);

        return submission.Status == SubmissionStatus.Draft
            ? submission
            : throw new SubmissionAlreadySubmittedException(submissionId);
    }

    private async Task SaveAsync(SubmissionDocument submission, CancellationToken cancellationToken)
    {
        var expectedVersion = submission.Version;
        submission.Version = expectedVersion + 1;

        if (!await repository.TryReplaceDraftAsync(submission, expectedVersion, cancellationToken))
        {
            throw new SubmissionChangedException(submission.HashId);
        }
    }
}
