// <copyright file="BusinessRuleException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Represents a submission business rule violation.
/// </summary>
public class BusinessRuleException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BusinessRuleException(string message)
        : base(message)
    {
    }
}
