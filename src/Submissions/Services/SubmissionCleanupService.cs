// <copyright file="SubmissionCleanupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Services;

using Defra.Livestock.Sdk.Submissions.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Deletes expired drafts and optionally errors at startup and hourly, retrying failures on the next pass.
/// </summary>
/// <param name="repository">The submission repository.</param>
/// <param name="timeProvider">The clock used for the retention cutoff and schedule.</param>
/// <param name="logger">The cleanup logger.</param>
/// <param name="options">The draft and error expiry windows.</param>
internal sealed partial class SubmissionCleanupService(
    ISubmissionRepository repository,
    TimeProvider timeProvider,
    ILogger<SubmissionCleanupService> logger,
    IOptions<SubmissionOptions> options) : BackgroundService
{
    internal async Task<long> CleanupAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var deleted = await repository.DeleteExpiredDraftsAsync((now - settings.DraftExpiry).UtcDateTime, cancellationToken);
        if (settings.ErrorExpiry is { } errorExpiry)
        {
            deleted += await repository.DeleteExpiredErrorsAsync((now - errorExpiry).UtcDateTime, cancellationToken);
        }

        return deleted;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1), timeProvider);
        do
        {
            try
            {
                var deleted = await CleanupAsync(stoppingToken);
                LogCleanedUpDrafts(deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                LogCleanupFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
