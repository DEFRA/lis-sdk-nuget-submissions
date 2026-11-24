// <copyright file="AnimalResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// An animal's status and data as returned to the caller.
/// </summary>
public sealed class AnimalResponse
{
    /// <summary>Gets the animal's id.</summary>
    [JsonPropertyName("animal-id")]
    [Description("The animal's ID.")]
    public required string AnimalId { get; init; }

    /// <summary>Gets the animal's status.</summary>
    [Description("The animal's status")]
    public required ItemStatus Status { get; init; }

    /// <summary>Gets the animal's data items.</summary>
    [Description("The data items of the animal.")]
    public required DataItem[] Data { get; init; }
}
