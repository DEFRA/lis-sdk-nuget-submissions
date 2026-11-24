// <copyright file="EmptySubmissionException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents empty submission.
/// </summary>
public sealed class EmptySubmissionException : BusinessRuleException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptySubmissionException"/> class.
    /// </summary>
    /// <param name="submissionId">The submissionId.</param>
    public EmptySubmissionException(string submissionId)
        : base($"Submission '{submissionId}' has no animals to submit.")
    {
    }
}
