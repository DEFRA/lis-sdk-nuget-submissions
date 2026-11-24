// <copyright file="ISubmissionTransfer.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Services;

using Defra.Livestock.Sdk.Submissions.Models;

/// <summary>
/// Transfers a completed submission downstream. Each host supplies its own, for example to the cattle API.
/// </summary>
public interface ISubmissionTransfer
{
    /// <summary>
    /// Transfers the submission. Successful completion confirms downstream receipt; the SDK retains the submitted record until its retention window expires. Throw <see cref="Core.Exceptions.ServiceUnavailableException"/> when the downstream
    /// service cannot take it right now, so the submission stays a draft and the caller can try again.
    /// </summary>
    /// <param name="submission">The draft being submitted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task TransferAsync(SubmissionTransferRequest submission, CancellationToken cancellationToken = default);
}
