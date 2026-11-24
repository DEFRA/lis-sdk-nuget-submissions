// <copyright file="DataItem.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using System.ComponentModel;

/// <summary>
/// One typed key and value of an animal's data.
/// </summary>
public sealed class DataItem
{
    /// <summary>Gets the key of the data item.</summary>
    [Description("The key of the data item.")]
    public required string Key { get; init; }

    /// <summary>Gets the value of the data item.</summary>
    [Description("The value of the data item.")]
    public required string? Value { get; init; }

    /// <summary>Gets the type of the data item, one of the <see cref="PropertyTypes"/>.</summary>
    [Description("The type of the data item.")]
    public required string Type { get; init; }
}
