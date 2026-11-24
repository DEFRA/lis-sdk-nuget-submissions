// <copyright file="UnsupportedItemStatusException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents unsupported item status.
/// </summary>
public sealed class UnsupportedItemStatusException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedItemStatusException"/> class.
    /// </summary>
    /// <param name="paramName">The name of the parameter carrying the invalid value.</param>
    public UnsupportedItemStatusException(string paramName)
        : base("The item status is not supported.", paramName)
    {
    }
}
