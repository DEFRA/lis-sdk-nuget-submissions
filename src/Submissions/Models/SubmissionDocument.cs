// <copyright file="SubmissionDocument.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using MongoDB.Bson.Serialization.Attributes;

/// <summary>
/// A submission as stored in MongoDB.
/// </summary>
internal sealed class SubmissionDocument
{
    /// <summary>Gets the submission's GUID. It is never returned to callers.</summary>
    [BsonId]
    public required string Id { get; init; }

    /// <summary>Gets the id callers use for the submission, a display hash of <see cref="Id"/>.</summary>
    [BsonElement("hashId")]
    public required string HashId { get; init; }

    /// <summary>Gets the county/parish/holding number, as <c>12/345/6789</c>.</summary>
    [BsonElement("cph")]
    public required string Cph { get; init; }

    /// <summary>Gets the UTC date and time the submission was created.</summary>
    [BsonElement("createdDate")]
    public required DateTime CreatedDate { get; init; }

    /// <summary>Gets or sets the UTC date and time the submission was submitted.</summary>
    [BsonElement("submittedDate")]
    public DateTime? SubmittedDate { get; set; }

    /// <summary>Gets or sets the submission's status, one of the <see cref="SubmissionStatus"/> values.</summary>
    [BsonElement("status")]
    public required string Status { get; set; }

    /// <summary>Gets or sets the version used to detect concurrent changes.</summary>
    [BsonElement("version")]
    public int Version { get; set; }

    /// <summary>Gets or sets the submission's animals.</summary>
    [BsonElement("animals")]
    public List<SubmissionAnimalDocument> Animals { get; set; } = [];
}
