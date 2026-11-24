// <copyright file="SubmissionEmptyException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents empty submission.
/// </summary>
public sealed class SubmissionEmptyException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionEmptyException"/> class.
    /// </summary>
    /// <param name="submissionId">The submissionId.</param>
    public SubmissionEmptyException(string submissionId)
        : base($"Submission '{submissionId}' has no animals to submit.")
    {
    }
}
