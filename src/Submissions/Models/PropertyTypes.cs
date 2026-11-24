// <copyright file="PropertyTypes.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

/// <summary>
/// The types a data item can have.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "The names match the type values callers send.")]
public static class PropertyTypes
{
    /// <summary>A string value.</summary>
    public const string String = "STRING";

    /// <summary>A date value.</summary>
    public const string Date = "DATE";

    /// <summary>An integer value.</summary>
    public const string Int = "INT";

    /// <summary>A numeric value.</summary>
    public const string Number = "NUMBER";

    /// <summary>A boolean value.</summary>
    public const string Bool = "BOOL";

    /// <summary>
    /// Returns whether a type is one of the supported types.
    /// </summary>
    /// <param name="type">The upper-case type.</param>
    /// <returns>True when the type is supported.</returns>
    public static bool IsSupported(string type) => type is String or Date or Int or Number or Bool;
}
