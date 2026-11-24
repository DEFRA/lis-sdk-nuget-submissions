// <copyright file="SubmissionNotFoundException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

using Defra.Livestock.Sdk.Core.Exceptions;

/// <summary>
/// Represents submission not found.
/// </summary>
public sealed class SubmissionNotFoundException : NotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionNotFoundException"/> class.
    /// </summary>
    /// <param name="submissionId">The submissionId.</param>
    public SubmissionNotFoundException(string submissionId)
        : base($"Submission '{submissionId}' was not found.")
    {
    }
}
