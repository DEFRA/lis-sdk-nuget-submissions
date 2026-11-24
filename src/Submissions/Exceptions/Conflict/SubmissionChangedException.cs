// <copyright file="SubmissionChangedException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents submission changed.
/// </summary>
public sealed class SubmissionChangedException : SubmissionConflictException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionChangedException"/> class.
    /// </summary>
    /// <param name="submissionId">The submissionId.</param>
    public SubmissionChangedException(string submissionId)
        : base($"Submission '{submissionId}' was changed by another request. Reload it and try again.")
    {
    }
}
