// <copyright file="ISubmissionRepository.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Storage;

using Defra.Livestock.Sdk.Submissions.Models;

/// <summary>
/// Stores submissions.
/// </summary>
internal interface ISubmissionRepository
{
    /// <summary>
    /// Finds the latest draft for a CPH.
    /// </summary>
    /// <param name="cph">The county parish holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest draft, or null when the CPH has no draft.</returns>
    Task<SubmissionDocument?> FindDraftAsync(string cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new draft.
    /// </summary>
    /// <param name="submission">The draft.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>False when another submission already has the hash id.</returns>
    Task<bool> TryInsertDraftAsync(SubmissionDocument submission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a submission by its hash id.
    /// </summary>
    /// <param name="hashId">The submission hash id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The submission, or null when no matching submission exists.</returns>
    Task<SubmissionDocument?> GetByHashIdAsync(string hashId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists submissions for a CPH, ordered by creation date from newest to oldest.
    /// </summary>
    /// <param name="cph">The county parish holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The submissions for the CPH, or an empty collection when none exist.</returns>
    Task<IReadOnlyCollection<SubmissionDocument>> ListAsync(string cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a draft, as long as it is still a draft at <paramref name="expectedVersion"/>.
    /// </summary>
    /// <param name="submission">The changed draft.</param>
    /// <param name="expectedVersion">The version that was read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>False when the stored submission has changed since it was read.</returns>
    Task<bool> TryReplaceDraftAsync(SubmissionDocument submission, int expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>Deletes drafts created on or before the UTC cutoff. Other statuses are retained.</summary>
    /// <param name="cutoffUtc">The inclusive creation date cutoff.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted drafts.</returns>
    Task<long> DeleteExpiredDraftsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    /// <summary>Deletes only the confirmed submitted version, retaining drafts and errors.</summary>
    /// <param name="id">The internal submission id.</param>
    /// <param name="expectedVersion">The submitted version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether that submitted version was deleted.</returns>
    Task<bool> DeleteSubmittedAsync(string id, int expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>Deletes error submissions created on or before the UTC cutoff. Other statuses are retained.</summary>
    /// <param name="cutoffUtc">The inclusive creation date cutoff.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted errors.</returns>
    Task<long> DeleteExpiredErrorsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
