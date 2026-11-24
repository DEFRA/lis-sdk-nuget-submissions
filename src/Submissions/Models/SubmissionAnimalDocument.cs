// <copyright file="SubmissionAnimalDocument.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

/// <summary>
/// An animal as stored in a <see cref="SubmissionDocument"/>.
/// </summary>
internal sealed class SubmissionAnimalDocument
{
    /// <summary>Gets the animal's id.</summary>
    [BsonElement("animalId")]
    public required string AnimalId { get; init; }

    /// <summary>Gets or sets the animal's status.</summary>
    [BsonRepresentation(BsonType.String)]
    [BsonElement("status")]
    public required ItemStatus Status { get; set; }

    /// <summary>Gets or sets the animal's data items.</summary>
    [BsonElement("data")]
    public List<SubmissionDataItemDocument> Data { get; set; } = [];
}
