// <copyright file="SubmissionMongoConventionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Storage;

using Defra.Livestock.Sdk.Submissions.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

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

    /// <summary>Reads the established camel-case schema without requiring convention registration first.</summary>
    [Fact]
    public void ShouldReadExistingCamelCaseDocumentsWithoutRegistration()
    {
        var bson = BsonDocument.Parse("""
            {
                "_id": "guid",
                "hashId": "ABC-DEF-GHJ",
                "cph": "12/345/6789",
                "createdDate": ISODate("2026-11-24T12:00:00Z"),
                "submittedDate": ISODate("2026-11-24T13:00:00Z"),
                "status": "SUBMITTED",
                "version": 7,
                "animals": [{
                    "animalId": "UK123",
                    "status": "Complete",
                    "data": [{ "key": "weight", "value": "42", "type": "INT" }]
                }]
            }
            """);

        var submission = BsonSerializer.Deserialize<SubmissionDocument>(bson);

        submission.Id.ShouldBe("guid");
        submission.HashId.ShouldBe("ABC-DEF-GHJ");
        submission.Cph.ShouldBe("12/345/6789");
        submission.CreatedDate.ShouldBe(new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc));
        submission.SubmittedDate.ShouldBe(new DateTime(2026, 11, 24, 13, 0, 0, DateTimeKind.Utc));
        submission.Status.ShouldBe(SubmissionStatus.Submitted);
        submission.Version.ShouldBe(7);
        var animal = submission.Animals.ShouldHaveSingleItem();
        animal.AnimalId.ShouldBe("UK123");
        animal.Status.ShouldBe(ItemStatus.Complete);
        var item = animal.Data.ShouldHaveSingleItem();
        item.Key.ShouldBe("weight");
        item.Value.ShouldBe("42");
        item.Type.ShouldBe(PropertyTypes.Int);
        submission.ToBsonDocument().ShouldBe(bson);
    }
}
