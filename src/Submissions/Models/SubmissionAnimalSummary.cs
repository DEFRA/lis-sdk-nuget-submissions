// <copyright file="SubmissionAnimalSummary.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// The id and status of an animal in a submission.
/// </summary>
public sealed class SubmissionAnimalSummary
{
    /// <summary>Gets the animal's id.</summary>
    [JsonPropertyName("animal-id")]
    [Description("The animal's ID")]
    public required string AnimalId { get; init; }

    /// <summary>Gets the animal's status.</summary>
    [Description("The animal's status")]
    public required ItemStatus Status { get; init; }
}
