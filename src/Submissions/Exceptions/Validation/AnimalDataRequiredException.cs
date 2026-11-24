// <copyright file="AnimalDataRequiredException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents animal data required.
/// </summary>
public sealed class AnimalDataRequiredException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimalDataRequiredException"/> class.
    /// </summary>
    /// <param name="paramName">The name of the parameter carrying the invalid value.</param>
    public AnimalDataRequiredException(string paramName)
        : base("Data must be an array of key, value and type items.", paramName)
    {
    }
}
