// <copyright file="ItemStatusTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Models;

using System.Text.Json;
using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

/// <summary>
/// Tests for ItemStatus.
/// </summary>
public sealed class ItemStatusTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemStatusTests"/> class.
    /// </summary>
    public ItemStatusTests()
    {
        SubmissionMongoConventions.Register();
    }

    /// <summary>
    /// Verifies: Should write canonical json strings.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <param name="expected">The expected.</param>
    [Theory]
    [InlineData(ItemStatus.Draft, "draft")]
    [InlineData(ItemStatus.Complete, "complete")]
    [InlineData(ItemStatus.Error, "error")]
    public void ShouldWriteCanonicalJsonStrings(ItemStatus status, string expected)
    {
        JsonSerializer.Serialize(status).ShouldBe($"\"{expected}\"");
    }

    /// <summary>
    /// Verifies: Should reject invalid json values.
    /// </summary>
    /// <param name="json">The json.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("999")]
    [InlineData("\"0\"")]
    [InlineData("\"999\"")]
    [InlineData("\"OTHER\"")]
    [InlineData("\"inProgress\"")]
    [InlineData("\"InProgress\"")]
    [InlineData("null")]
    public void ShouldRejectInvalidJsonValues(string json)
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ItemStatus>(json));
    }

    /// <summary>
    /// Verifies: Should preserve existing bson strings.
    /// </summary>
    /// <param name="storedStatus">The stored status.</param>
    /// <param name="expected">The expected.</param>
    [Theory]
    [InlineData("DRAFT", ItemStatus.Draft)]
    [InlineData("dRAfT", ItemStatus.Draft)]
    [InlineData("COMPLETE", ItemStatus.Complete)]
    [InlineData("COmPLEtE", ItemStatus.Complete)]
    public void ShouldPreserveExistingBsonStrings(string storedStatus, ItemStatus expected)
    {
        var bson = new BsonDocument
        {
            { "animalId", "UK1" },
            { "status", storedStatus },
            { "data", new BsonArray() },
        };

        var animal = BsonSerializer.Deserialize<SubmissionAnimalDocument>(bson);

        animal.Status.ShouldBe(expected);
        animal.ToBsonDocument()["status"].AsString.ShouldBe(expected.ToString());
    }
}
