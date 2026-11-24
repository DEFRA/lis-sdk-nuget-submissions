// <copyright file="UnsupportedDataItemTypeException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents unsupported data item type.
/// </summary>
public sealed class UnsupportedDataItemTypeException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedDataItemTypeException"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
    /// <param name="paramName">The name of the parameter carrying the invalid value.</param>
    public UnsupportedDataItemTypeException(string key, string? type, string paramName)
        : base($"Data item '{key}' has an unsupported type '{type}'.", paramName)
    {
    }
}
