// <copyright file="DataItemKeyRequiredException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents data item key required.
/// </summary>
public sealed class DataItemKeyRequiredException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataItemKeyRequiredException"/> class.
    /// </summary>
    /// <param name="paramName">The name of the parameter carrying the invalid value.</param>
    public DataItemKeyRequiredException(string paramName)
        : base("Every data item needs a key.", paramName)
    {
    }
}
