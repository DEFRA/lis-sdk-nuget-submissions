// <copyright file="NotConfiguredSubmissionTransferTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Services;

using Defra.Livestock.Sdk.Core.Exceptions;
using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Services;

/// <summary>
/// Tests for NotConfiguredSubmissionTransfer.
/// </summary>
public sealed class NotConfiguredSubmissionTransferTests
{
    /// <summary>
    /// Verifies: Should always fail as unavailable so the submission stays a draft.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ShouldAlwaysFailAsUnavailableSoTheSubmissionStaysADraft()
    {
        var submission = new SubmissionTransferRequest
        {
            SubmissionId = "ABC-DEF-GHJ",
            Cph = "12/345/6789",
            CreatedDate = DateTime.UtcNow,
            Animals = [],
        };

        await Should.ThrowAsync<ServiceUnavailableException>(
            () => new NotConfiguredSubmissionTransfer().TransferAsync(submission, TestContext.Current.CancellationToken));
    }
}
