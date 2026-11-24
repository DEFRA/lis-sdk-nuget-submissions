// <copyright file="SubmissionMongoConventionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Storage;

using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Storage;
using MongoDB.Bson;

/// <summary>
/// Tests for SubmissionMongoConventions.
/// </summary>
public sealed class SubmissionMongoConventionsTests
{
    /// <summary>
    /// Verifies: Should store the document shape cattle register already uses.
    /// </summary>
    [Fact]
    public void ShouldStoreTheDocumentShapeCattleRegisterAlreadyUses()
    {
        SubmissionMongoConventions.Register();
        var submission = new SubmissionDocument
        {
            Id = "guid",
            HashId = "PRS-PJY-WV8",
            Cph = "12/345/6789",
            CreatedDate = new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc),
            Status = SubmissionStatus.Draft,
            Animals =
            [
                new SubmissionAnimalDocument
                {
                    AnimalId = "UK1",
                    Status = ItemStatus.Complete,
                    Data = [new SubmissionDataItemDocument { Key = "dob", Value = "2026-11-01", Type = PropertyTypes.Date }],
                },
            ],
        };

        var bson = submission.ToBsonDocument();

        bson.Names.ShouldBe(["_id", "hashId", "cph", "createdDate", "submittedDate", "status", "version", "animals"]);
        var animal = bson["animals"][0].AsBsonDocument;
        animal.Names.ShouldBe(["animalId", "status", "data"]);
        animal["status"].AsString.ShouldBe("Complete");
        animal["data"][0].AsBsonDocument.Names.ShouldBe(["key", "value", "type"]);
    }
}
