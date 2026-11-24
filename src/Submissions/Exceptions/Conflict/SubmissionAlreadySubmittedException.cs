// <copyright file="SubmissionAlreadySubmittedException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents submission already submitted.
/// </summary>
public sealed class SubmissionAlreadySubmittedException : SubmissionConflictException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionAlreadySubmittedException"/> class.
    /// </summary>
    /// <param name="submissionId">The submissionId.</param>
    public SubmissionAlreadySubmittedException(string submissionId)
        : base($"Submission '{submissionId}' has already been submitted.")
    {
    }
}
