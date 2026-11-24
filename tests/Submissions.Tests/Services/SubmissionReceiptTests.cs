// <copyright file="SubmissionReceiptTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Services;

using Defra.Livestock.Sdk.Submissions.Exceptions;
using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Services;
using Defra.Livestock.Sdk.Submissions.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

/// <summary>Verifies successful transfers are timestamped and retained for scheduled cleanup.</summary>
public sealed class SubmissionReceiptTests
{
    /// <summary>Retains a pending submission until the transfer task confirms receipt.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldWaitForReceiptBeforeSavingSubmittedState()
    {
        var repository = Substitute.For<ISubmissionRepository>();
        var transfer = Substitute.For<ISubmissionTransfer>();
        var submission = CreateSubmission();
        var token = TestContext.Current.CancellationToken;
        var receipt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.GetByHashIdAsync(submission.HashId, token).Returns(submission);
        repository.TryReplaceDraftAsync(submission, 0, token).Returns(true);
        transfer.TransferAsync(Arg.Any<SubmissionTransferRequest>(), token).Returns(receipt.Task);
        var service = new SubmissionService(repository, transfer, TimeProvider.System, NullLogger<SubmissionService>.Instance);

        var pending = service.SubmitAsync(submission.Cph, submission.HashId, token);

        pending.IsCompleted.ShouldBeFalse();
        await repository.DidNotReceive().TryReplaceDraftAsync(Arg.Any<SubmissionDocument>(), Arg.Any<int>(), token);
        await repository.DidNotReceive().DeleteSubmittedAsync(Arg.Any<string>(), Arg.Any<int>(), token);

        receipt.SetResult();
        await pending;

        submission.Status.ShouldBe(SubmissionStatus.Submitted);
        submission.SubmittedDate.ShouldNotBeNull();
        await repository.Received(1).TryReplaceDraftAsync(submission, 0, token);
        await repository.DidNotReceive().DeleteSubmittedAsync(Arg.Any<string>(), Arg.Any<int>(), token);
        await repository.DidNotReceive().DeleteExpiredSubmittedAsync(Arg.Any<DateTime>(), token);
    }

    /// <summary>A failed state save must not trigger deletion after transfer.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldNotDeleteWhenSubmittedStateCannotBeSaved()
    {
        var repository = Substitute.For<ISubmissionRepository>();
        var transfer = Substitute.For<ISubmissionTransfer>();
        var submission = CreateSubmission();
        var token = TestContext.Current.CancellationToken;
        repository.GetByHashIdAsync(submission.HashId, token).Returns(submission);
        repository.TryReplaceDraftAsync(submission, 0, token).Returns(false);
        var service = new SubmissionService(repository, transfer, TimeProvider.System, NullLogger<SubmissionService>.Instance);

        await Should.ThrowAsync<SubmissionChangedException>(() => service.SubmitAsync(submission.Cph, submission.HashId, token));

        await repository.DidNotReceive().DeleteSubmittedAsync(Arg.Any<string>(), Arg.Any<int>(), token);
    }

    private static SubmissionDocument CreateSubmission() => new()
    {
        Id = "internal-id",
        HashId = "ABC-DEF-GHJ",
        Cph = "12/345/6789",
        CreatedDate = DateTime.UtcNow,
        Status = SubmissionStatus.Draft,
        Animals = [new SubmissionAnimalDocument { AnimalId = "UK123", Status = ItemStatus.Complete }],
    };
}
