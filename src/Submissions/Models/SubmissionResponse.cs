// <copyright file="SubmissionResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// A submission as returned to the caller.
/// </summary>
public sealed class SubmissionResponse
{
    /// <summary>Gets the submission's id.</summary>
    [JsonPropertyName("submission-id")]
    [Description("The submission's ID.")]
    public required string SubmissionId { get; init; }

    /// <summary>Gets the number of animals in the submission.</summary>
    [JsonPropertyName("animal-count")]
    [Description("The number of animals in the submission.")]
    public required int AnimalCount { get; init; }

    /// <summary>Gets the submission's animals.</summary>
    [Description("The submission's animals.")]
    public IReadOnlyCollection<SubmissionAnimalSummary> Animals { get; init; } = [];

    /// <summary>Gets the submission's status.</summary>
    [Description("The submission's status.")]
    public required string Status { get; init; }

    /// <summary>Gets the date and time the submission was created.</summary>
    [JsonPropertyName("created-date")]
    [Description("The date and time the submission was created.")]
    public required DateTimeOffset CreatedDate { get; init; }

    /// <summary>Gets the date and time the submission was submitted.</summary>
    [JsonPropertyName("submitted-date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("The date and time the submission was submitted.")]
    public DateTimeOffset? SubmittedDate { get; init; }
}
