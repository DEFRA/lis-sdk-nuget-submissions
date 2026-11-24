// <copyright file="NullDataItemException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents null data item.
/// </summary>
public sealed class NullDataItemException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullDataItemException"/> class.
    /// </summary>
    /// <param name="paramName">The name of the parameter carrying the invalid value.</param>
    public NullDataItemException(string paramName)
        : base("Data items must not be null.", paramName)
    {
    }
}
