// <copyright file="SubmissionDataItemDocument.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using MongoDB.Bson.Serialization.Attributes;

/// <summary>
/// A data item as stored in a <see cref="SubmissionAnimalDocument"/>.
/// </summary>
internal sealed class SubmissionDataItemDocument
{
    /// <summary>Gets the key of the data item.</summary>
    [BsonElement("key")]
    public required string Key { get; init; }

    /// <summary>Gets the value of the data item.</summary>
    [BsonElement("value")]
    public required string? Value { get; init; }

    /// <summary>Gets the upper-case type of the data item.</summary>
    [BsonElement("type")]
    public required string Type { get; init; }
}
