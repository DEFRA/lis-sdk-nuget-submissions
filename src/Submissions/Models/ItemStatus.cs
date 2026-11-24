// <copyright file="ItemStatus.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using System.Text.Json.Serialization;

/// <summary>
/// The status of an animal in a submission.
/// </summary>
[JsonConverter(typeof(ItemStatusJsonConverter))]
public enum ItemStatus
{
    /// <summary>The animal's data is incomplete.</summary>
    Draft,

    /// <summary>The animal's data is complete.</summary>
    Complete,

    /// <summary>The animal's data has an error.</summary>
    Error,
}
