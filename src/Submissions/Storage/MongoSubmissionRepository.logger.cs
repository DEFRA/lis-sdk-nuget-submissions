// <copyright file="MongoSubmissionRepository.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Storage;

using Microsoft.Extensions.Logging;

/// <content>
/// The log messages of the <see cref="MongoSubmissionRepository"/>.
/// </content>
internal sealed partial class MongoSubmissionRepository
{
    [LoggerMessage(LogLevel.Warning, "Unable to create Mongo submission indexes. Continuing without them; the next call retries.")]
    private partial void LogIndexCreationFailed(Exception exception);
}
