// <copyright file="AnimalRequest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using System.ComponentModel;

/// <summary>
/// The body of a request that saves an animal's status and data.
/// </summary>
public sealed class AnimalRequest
{
    /// <summary>Gets the animal's status.</summary>
    [Description("The animal's status")]
    public required ItemStatus Status { get; init; }

    /// <summary>Gets the animal's data items.</summary>
    [Description("The data items of the animal.")]
    public required DataItem[] Data { get; init; }
}
