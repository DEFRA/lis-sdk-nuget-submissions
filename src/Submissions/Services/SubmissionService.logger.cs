// <copyright file="SubmissionService.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Services;

using Microsoft.Extensions.Logging;

/// <content>
/// The log messages of the <see cref="SubmissionService"/>.
/// </content>
internal sealed partial class SubmissionService
{
    [LoggerMessage(LogLevel.Information, "Created a draft submission. SubmissionId: {SubmissionId}, Cph: {Cph}")]
    partial void LogCreatedDraftSubmission(string submissionId, string cph);

    [LoggerMessage(LogLevel.Information, "Deleted an animal from a draft submission, leaving {AnimalCount} animals. SubmissionId: {SubmissionId}")]
    partial void LogDeletedAnimal(string submissionId, int animalCount);

    [LoggerMessage(LogLevel.Information, "Submitted a submission with {AnimalCount} animals. SubmissionId: {SubmissionId}")]
    partial void LogSubmittedSubmission(string submissionId, int animalCount);

    [LoggerMessage(LogLevel.Warning, "The submission could not be transferred and stays a draft. SubmissionId: {SubmissionId}")]
    partial void LogSubmissionTransferFailed(string submissionId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "The requested submission does not belong to the CPH. SubmissionId: {SubmissionId}, Cph: {Cph}")]
    partial void LogRequestedSubmissionDoesNotBelongToCph(string submissionId, string cph);
}
