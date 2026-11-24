// <copyright file="DuplicateDataItemException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents duplicate data item.
/// </summary>
public sealed class DuplicateDataItemException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateDataItemException"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="paramName">The name of the parameter carrying the invalid value.</param>
    public DuplicateDataItemException(string key, string paramName)
        : base($"Data item '{key}' appears more than once.", paramName)
    {
    }
}
