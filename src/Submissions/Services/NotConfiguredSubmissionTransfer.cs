// <copyright file="NotConfiguredSubmissionTransfer.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Services;

using Defra.Livestock.Sdk.Submissions.Exceptions;

using Defra.Livestock.Sdk.Submissions.Models;

/// <summary>
/// Stands in until a host's transfer is built. It always fails, so a submission stays a draft rather than being marked
/// as submitted when nothing was sent.
/// </summary>
public sealed class NotConfiguredSubmissionTransfer : ISubmissionTransfer
{
    /// <inheritdoc/>
    /// <exception cref="SubmissionTransferNotConfiguredException">Always.</exception>
    public Task TransferAsync(SubmissionTransferRequest submission, CancellationToken cancellationToken = default)
    {
        throw new SubmissionTransferNotConfiguredException();
    }
}
