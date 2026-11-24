// <copyright file="AnimalNotFoundException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

using Defra.Livestock.Sdk.Core.Exceptions;

/// <summary>
/// Represents animal not found.
/// </summary>
public sealed class AnimalNotFoundException : NotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimalNotFoundException"/> class.
    /// </summary>
    /// <param name="animalId">The animalId.</param>
    /// <param name="submissionId">The submissionId.</param>
    public AnimalNotFoundException(string animalId, string submissionId)
        : base($"Animal '{animalId}' was not found in submission '{submissionId}'.")
    {
    }
}
