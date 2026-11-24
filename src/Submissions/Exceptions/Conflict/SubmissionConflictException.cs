// <copyright file="SubmissionConflictException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents submission conflict.
/// </summary>
public class SubmissionConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionConflictException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public SubmissionConflictException(string message)
        : base(message)
    {
    }
}
