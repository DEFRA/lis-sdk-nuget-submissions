// <copyright file="SubmissionCleanupService.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Services;

using Microsoft.Extensions.Logging;

/// <content>
/// The log messages of the <see cref="SubmissionCleanupService"/>.
/// </content>
internal sealed partial class SubmissionCleanupService
{
    [LoggerMessage(LogLevel.Information, "Deleted {Count} expired submissions.")]
    private partial void LogCleanedUpDrafts(long count);

    [LoggerMessage(LogLevel.Warning, "Unable to clean up expired submissions. The next hourly pass retries.")]
    private partial void LogCleanupFailed(Exception exception);
}
