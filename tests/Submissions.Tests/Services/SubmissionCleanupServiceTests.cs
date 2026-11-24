// <copyright file="SubmissionCleanupServiceTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Services;

using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Services;
using Defra.Livestock.Sdk.Submissions.Storage;
using Defra.Livestock.Sdk.Submissions.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

/// <summary>Verifies the retention cutoff and protection of non-draft submissions.</summary>
public sealed class SubmissionCleanupServiceTests
{
    /// <summary>Uses the injected clock and forwards cancellation and deletion count.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldUseThirtyDayUtcCutoff()
    {
        var now = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        var repository = Substitute.For<ISubmissionRepository>();
        var token = TestContext.Current.CancellationToken;
        repository.DeleteExpiredDraftsAsync(now.AddDays(-30).UtcDateTime, token).Returns(2L);
        using var service = new SubmissionCleanupService(repository, new FixedTimeProvider(now), NullLogger<SubmissionCleanupService>.Instance, Microsoft.Extensions.Options.Options.Create(new Defra.Livestock.Sdk.Submissions.SubmissionOptions()));

        (await service.CleanupAsync(token)).ShouldBe(2);

        await repository.Received(1).DeleteExpiredDraftsAsync(now.AddDays(-30).UtcDateTime, token);
    }

    /// <summary>Deletes drafts at thirty days, retaining recent drafts, submissions and errors.</summary>
    /// <param name="status">The stored status.</param>
    /// <param name="ageDays">The age at cleanup.</param>
    /// <param name="deleted">Whether the document should be deleted.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(SubmissionStatus.Draft, 29, false)]
    [InlineData(SubmissionStatus.Draft, 30, true)]
    [InlineData(SubmissionStatus.Draft, 31, true)]
    [InlineData(SubmissionStatus.Submitted, 60, false)]
    [InlineData(SubmissionStatus.Error, 60, false)]
    public async Task ShouldRespectAgeAndStatus(string status, int ageDays, bool deleted)
    {
        var now = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        var repository = new InMemorySubmissionRepository();
        var token = TestContext.Current.CancellationToken;
        var submission = new SubmissionDocument
        {
            Id = "id",
            HashId = "ABC-DEF-GHJ",
            Cph = "12/345/6789",
            CreatedDate = now.AddDays(-ageDays).UtcDateTime,
            Status = status,
        };
        await repository.TryInsertDraftAsync(submission, token);
        using var service = new SubmissionCleanupService(repository, new FixedTimeProvider(now), NullLogger<SubmissionCleanupService>.Instance, Microsoft.Extensions.Options.Options.Create(new Defra.Livestock.Sdk.Submissions.SubmissionOptions()));

        (await service.CleanupAsync(token)).ShouldBe(deleted ? 1 : 0);
        repository.Stored.Count.ShouldBe(deleted ? 0 : 1);
    }

    /// <summary>Runs at startup and on the hourly timer, including after a failed pass.</summary>
    /// <param name="failFirst">Whether the first cleanup fails.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShouldRunAtStartupAndOnNextHourlyTick(bool failFirst)
    {
        var repository = Substitute.For<ISubmissionRepository>();
        var clock = new ControlledTimeProvider();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        repository.DeleteExpiredDraftsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    first.SetResult();
                    return failFirst ? Task.FromException<long>(new InvalidOperationException("Storage unavailable")) : Task.FromResult(0L);
                }

                second.SetResult();
                return Task.FromResult(0L);
            });
        using var service = new SubmissionCleanupService(repository, clock, NullLogger<SubmissionCleanupService>.Instance, Microsoft.Extensions.Options.Options.Create(new Defra.Livestock.Sdk.Submissions.SubmissionOptions()));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        await service.StartAsync(timeout.Token);
        try
        {
            await first.Task.WaitAsync(timeout.Token);
            clock.Period.ShouldBe(TimeSpan.FromHours(1));
            clock.Tick();
            await second.Task.WaitAsync(timeout.Token);
            attempts.ShouldBe(2);
        }
        finally
        {
            await service.StopAsync(timeout.Token);
        }
    }

    /// <summary>Applies separate configured expiry windows and combines the deletion counts.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldUseConfiguredDraftAndErrorWindows()
    {
        var now = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        var repository = Substitute.For<ISubmissionRepository>();
        var token = TestContext.Current.CancellationToken;
        var settings = new SubmissionOptions { DraftExpiry = TimeSpan.FromDays(7), ErrorExpiry = TimeSpan.FromDays(90) };
        repository.DeleteExpiredDraftsAsync(now.AddDays(-7).UtcDateTime, token).Returns(2L);
        repository.DeleteExpiredErrorsAsync(now.AddDays(-90).UtcDateTime, token).Returns(3L);
        using var service = new SubmissionCleanupService(
            repository,
            new FixedTimeProvider(now),
            NullLogger<SubmissionCleanupService>.Instance,
            Options.Create(settings));

        (await service.CleanupAsync(token)).ShouldBe(5);

        await repository.Received(1).DeleteExpiredDraftsAsync(now.AddDays(-7).UtcDateTime, token);
        await repository.Received(1).DeleteExpiredErrorsAsync(now.AddDays(-90).UtcDateTime, token);
    }

    /// <summary>Never calls error deletion unless error expiry is enabled.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldLeaveErrorCleanupDisabledByDefault()
    {
        var repository = Substitute.For<ISubmissionRepository>();
        using var service = new SubmissionCleanupService(
            repository,
            TimeProvider.System,
            NullLogger<SubmissionCleanupService>.Instance,
            Options.Create(new SubmissionOptions()));

        await service.CleanupAsync(TestContext.Current.CancellationToken);

        await repository.DidNotReceive().DeleteExpiredErrorsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Configured error expiry uses an inclusive cutoff and retains recent errors.</summary>
    /// <param name="status">The submission status.</param>
    /// <param name="ageDays">The creation age.</param>
    /// <param name="deleted">Whether cleanup should delete the document.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(SubmissionStatus.Error, 59, false)]
    [InlineData(SubmissionStatus.Error, 60, true)]
    [InlineData(SubmissionStatus.Error, 61, true)]
    [InlineData(SubmissionStatus.Submitted, 90, false)]
    public async Task ShouldRespectConfiguredErrorExpiry(string status, int ageDays, bool deleted)
    {
        var now = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        var repository = new InMemorySubmissionRepository();
        var token = TestContext.Current.CancellationToken;
        var submission = new SubmissionDocument
        {
            Id = "id",
            HashId = "ABC-DEF-GHJ",
            Cph = "12/345/6789",
            CreatedDate = now.AddDays(-ageDays).UtcDateTime,
            Status = status,
        };
        await repository.TryInsertDraftAsync(submission, token);
        using var service = new SubmissionCleanupService(
            repository,
            new FixedTimeProvider(now),
            NullLogger<SubmissionCleanupService>.Instance,
            Options.Create(new SubmissionOptions { ErrorExpiry = TimeSpan.FromDays(60) }));

        (await service.CleanupAsync(token)).ShouldBe(deleted ? 1 : 0);
        repository.Stored.Count.ShouldBe(deleted ? 0 : 1);
    }

    private sealed class ControlledTimeProvider : TimeProvider
    {
        private TimerCallback? callback;
        private object? state;

        public TimeSpan Period { get; private set; }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            this.callback = callback;
            this.state = state;
            Period = period;
            return new ControlledTimer();
        }

        public void Tick() => callback!(state);

        private sealed class ControlledTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
