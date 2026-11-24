// <copyright file="ISubmissionService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Services;

using Defra.Livestock.Sdk.Submissions.Models;

/// <summary>
/// The submission process: drafts per CPH, their animals, and submitting them.
/// </summary>
public interface ISubmissionService
{
    /// <summary>
    /// Lists the CPH's submissions, newest first.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The submissions.</returns>
    Task<SubmissionListResponse> ListAsync(string cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes the latest draft for the CPH, creating one if none exists.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The draft's id and summary.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "ApiDesign",
        "RS0027:API with optional parameter(s) should have the most parameters amongst its public overloads",
        Justification = "Preserves the shipped overload and its optional cancellation token for existing callers.")]
    Task<SubmissionResponse> StartAsync(string cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes the draft identified by the key, or the latest draft when the key is null, creating one if needed.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="key">The submission id to resume, or null to resume the latest draft.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The draft's id and summary.</returns>
    Task<SubmissionResponse> StartAsync(string cph, string? key, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the submission details.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="submissionId">The submission id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The submission and summary.</returns>
    Task<SubmissionResponse> GetSubmissionAsync(string cph, string submissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an animal's data, with empty data if nothing has been saved for it yet.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="submissionId">The submission id.</param>
    /// <param name="animalId">The animal id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The animal id, section name and data.</returns>
    Task<AnimalResponse> GetAnimalAsync(string cph, string submissionId, string animalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an animal's data, adding the animal to the draft if it is new.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="submissionId">The submission id.</param>
    /// <param name="animalId">The animal id.</param>
    /// <param name="request">The animal id, section name and data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpsertAnimalAsync(string cph, string submissionId, string animalId, AnimalRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an animal from a draft.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="submissionId">The submission id.</param>
    /// <param name="animalId">The animal id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAnimalAsync(string cph, string submissionId, string animalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers a draft downstream and retains it with submitted status and a timestamp after confirmed receipt.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="submissionId">The submission id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SubmitAsync(string cph, string submissionId, CancellationToken cancellationToken = default);
}
