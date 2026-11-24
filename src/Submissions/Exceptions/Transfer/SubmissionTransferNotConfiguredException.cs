// <copyright file="SubmissionTransferNotConfiguredException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

using Defra.Livestock.Sdk.Core.Exceptions;

/// <summary>
/// Represents submission transfer not configured.
/// </summary>
public sealed class SubmissionTransferNotConfiguredException : ServiceUnavailableException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionTransferNotConfiguredException"/> class.
    /// </summary>
    public SubmissionTransferNotConfiguredException()
        : base("Submitting is not available yet.")
    {
    }
}
