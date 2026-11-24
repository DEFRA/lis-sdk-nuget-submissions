// <copyright file="SubmissionTransferRequest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

/// <summary>
/// A submission sent downstream, independent of its storage representation.
/// </summary>
public sealed class SubmissionTransferRequest
{
    /// <summary>Gets the public submission id, which can be used for downstream correlation.</summary>
    public required string SubmissionId { get; init; }

    /// <summary>Gets the county/parish/holding number.</summary>
    public required string Cph { get; init; }

    /// <summary>Gets the submission's creation date.</summary>
    public required DateTimeOffset CreatedDate { get; init; }

    /// <summary>Gets all animals with their status and full data items.</summary>
    public required IReadOnlyCollection<AnimalResponse> Animals { get; init; }
}
