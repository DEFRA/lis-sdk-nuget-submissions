// <copyright file="DraftSubmissionCreationException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents draft submission creation.
/// </summary>
public sealed class DraftSubmissionCreationException : SubmissionConflictException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DraftSubmissionCreationException"/> class.
    /// </summary>
    /// <param name="cph">The cph.</param>
    public DraftSubmissionCreationException(string cph)
        : base($"A draft submission for CPH '{cph}' could not be started. Try again.")
    {
    }
}
