// <copyright file="AnimalIdRequiredException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents animal id required.
/// </summary>
public sealed class AnimalIdRequiredException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimalIdRequiredException"/> class.
    /// </summary>
    /// <param name="paramName">The name of the parameter carrying the invalid value.</param>
    public AnimalIdRequiredException(string paramName)
        : base("An animal id is required.", paramName)
    {
    }
}
