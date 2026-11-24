// <copyright file="InvalidCphException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents invalid cph.
/// </summary>
public sealed class InvalidCphException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCphException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="paramName">The name of the parameter carrying the invalid value.</param>
    public InvalidCphException(string message, string paramName)
        : base(message, paramName)
    {
    }
}
