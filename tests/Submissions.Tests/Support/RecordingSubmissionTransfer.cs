// <copyright file="RecordingSubmissionTransfer.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Support;

using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Services;

internal sealed class RecordingSubmissionTransfer : ISubmissionTransfer
{
    public Exception? FailWith { get; set; }

    public List<string> TransferredIds { get; } = [];

    public Task TransferAsync(SubmissionTransferRequest submission, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        TransferredIds.Add(submission.SubmissionId);
        return Task.CompletedTask;
    }
}
