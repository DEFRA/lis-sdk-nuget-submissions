// <copyright file="MongoSubmissionRepositoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Storage;

using System.Net;
using System.Reflection;
using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

/// <summary>
/// Verifies Mongo queries, writes and index lifecycle without a running database.
/// </summary>
public sealed class MongoSubmissionRepositoryTests
{
    private readonly IMongoDatabase database = Substitute.For<IMongoDatabase>();
    private readonly IMongoCollection<SubmissionDocument> collection = Substitute.For<IMongoCollection<SubmissionDocument>>();
    private readonly IMongoIndexManager<SubmissionDocument> indexes = Substitute.For<IMongoIndexManager<SubmissionDocument>>();
    private readonly MongoSubmissionRepository repository;
    private readonly RenderArgs<SubmissionDocument> renderArgs;
    private readonly CancellationToken token = TestContext.Current.CancellationToken;
    private SubmissionDocument[] results = [];
    private BsonDocument? query;
    private BsonDocument? sort;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoSubmissionRepositoryTests"/> class.
    /// </summary>
    public MongoSubmissionRepositoryTests()
    {
        SubmissionMongoConventions.Register();
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<SubmissionDocument>();
        renderArgs = new RenderArgs<SubmissionDocument>(serializer, BsonSerializer.SerializerRegistry);
        database.GetCollection<SubmissionDocument>("custom_submissions").Returns(collection);
        collection.DocumentSerializer.Returns(serializer);
        collection.Settings.Returns(new MongoCollectionSettings());
        collection.Indexes.Returns(indexes);
        indexes.CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<SubmissionDocument>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>([]));
        collection.FindAsync(
                Arg.Any<FilterDefinition<SubmissionDocument>>(),
                Arg.Any<FindOptions<SubmissionDocument, SubmissionDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                query = call.Arg<FilterDefinition<SubmissionDocument>>().Render(renderArgs);
                sort = call.Arg<FindOptions<SubmissionDocument, SubmissionDocument>>().Sort?.Render(renderArgs);
                var cursor = Substitute.For<IAsyncCursor<SubmissionDocument>>();
                cursor.Current.Returns(results);
                cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
                return Task.FromResult(cursor);
            });
        repository = new MongoSubmissionRepository(
            database,
            Options.Create(new SubmissionOptions { CollectionName = "custom_submissions" }),
            NullLogger<MongoSubmissionRepository>.Instance);
    }

    /// <summary>Finds only the requested holding's draft and returns the Mongo result.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldFindDraftByCphAndStatus()
    {
        var draft = CreateSubmission();
        results = [draft];

        (await repository.FindDraftAsync(draft.Cph, token)).ShouldBeSameAs(draft);

        query.ShouldBe(BsonDocument.Parse("{ cph: '12/345/6789', status: 'DRAFT' }"));
        sort.ShouldBe(new BsonDocument("createdDate", -1));
        await collection.Received(1).FindAsync(
            Arg.Any<FilterDefinition<SubmissionDocument>>(),
            Arg.Any<FindOptions<SubmissionDocument, SubmissionDocument>>(),
            token);
    }

    /// <summary>Returns null for an absent draft.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldReturnNullWhenDraftDoesNotExist()
    {
        (await repository.FindDraftAsync("12/345/6789", token)).ShouldBeNull();
    }

    /// <summary>Looks up the public hash rather than the Mongo id.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldFindSubmissionByHashId()
    {
        var submission = CreateSubmission();
        results = [submission];

        (await repository.GetByHashIdAsync(submission.HashId, token)).ShouldBeSameAs(submission);

        query.ShouldBe(new BsonDocument("hashId", submission.HashId));
    }

    /// <summary>Returns null for an unknown public hash.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldReturnNullWhenHashIdDoesNotExist()
    {
        (await repository.GetByHashIdAsync("missing", token)).ShouldBeNull();
    }

    /// <summary>Filters by holding and asks Mongo to sort newest first.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldListByCphWithDescendingCreatedDate()
    {
        results = [CreateSubmission(), CreateSubmission()];

        (await repository.ListAsync("12/345/6789", token)).ShouldBe(results);

        query.ShouldBe(new BsonDocument("cph", "12/345/6789"));
        sort.ShouldBe(new BsonDocument("createdDate", -1));
    }

    /// <summary>Returns an empty list when Mongo has no matching submissions.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldReturnEmptyList()
    {
        (await repository.ListAsync("12/345/6789", token)).ShouldBeEmpty();
    }

    /// <summary>Inserts the original document and forwards cancellation.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldInsertDraft()
    {
        var submission = CreateSubmission();

        (await repository.TryInsertDraftAsync(submission, token)).ShouldBeTrue();

        await collection.Received(1).InsertOneAsync(submission, Arg.Any<InsertOneOptions>(), token);
    }

    /// <summary>Does not disguise unrelated storage failures as duplicate drafts.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldPropagateInsertFailure()
    {
        var failure = new InvalidOperationException("Mongo unavailable");
        collection.InsertOneAsync(Arg.Any<SubmissionDocument>(), Arg.Any<InsertOneOptions>(), token).ThrowsAsync(failure);

        var actual = await Should.ThrowAsync<InvalidOperationException>(() => repository.TryInsertDraftAsync(CreateSubmission(), token));

        actual.ShouldBeSameAs(failure);
    }

    /// <summary>Matches id, draft status and expected version when replacing.</summary>
    /// <param name="matchedCount">The number of documents matching the version check.</param>
    /// <param name="expected">Whether replacement succeeded.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task ShouldReplaceOnlyTheExpectedDraftVersion(long matchedCount, bool expected)
    {
        var submission = CreateSubmission();
        submission.Version = 8;
        collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<SubmissionDocument>>(), submission, Arg.Any<ReplaceOptions>(), token)
            .Returns(new ReplaceOneResult.Acknowledged(matchedCount, 0, null));

        (await repository.TryReplaceDraftAsync(submission, 7, token)).ShouldBe(expected);

        await collection.Received(1).ReplaceOneAsync(
            Arg.Is<FilterDefinition<SubmissionDocument>>(filter => filter.Render(renderArgs).Equals(
                new BsonDocument { { "_id", submission.Id }, { "status", "DRAFT" }, { "version", 7 } })),
            submission,
            Arg.Any<ReplaceOptions>(),
            token);
    }

    /// <summary>Creates uniqueness and listing indexes once across repository operations.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldCreateIndexesOnlyOnce()
    {
        await repository.FindDraftAsync("12/345/6789", token);
        await repository.GetByHashIdAsync("hash", token);
        await repository.ListAsync("12/345/6789", token);
        await repository.TryInsertDraftAsync(CreateSubmission(), token);

        var call = indexes.ReceivedCalls().Single(received => received.GetMethodInfo().Name == nameof(indexes.CreateManyAsync));
        call.GetArguments()[1].ShouldBe(token);
        var models = ((IEnumerable<CreateIndexModel<SubmissionDocument>>)call.GetArguments()[0]!).ToArray();
        models.Length.ShouldBe(2);
        models[0].Options.Name.ShouldBe("unique_hashId");
        models[0].Options.Unique.ShouldBe(true);
        models[0].Keys.Render(renderArgs).ShouldBe(new BsonDocument("hashId", 1));
        models[1].Options.Name.ShouldBe("cph_createdDate");
        models[1].Options.Unique.ShouldNotBe(true);
        models[1].Keys.Render(renderArgs).ShouldBe(new BsonDocument { { "cph", 1 }, { "createdDate", -1 } });
        await indexes.Received(1).DropOneAsync("unique_draft_per_cph", token);
    }

    /// <summary>Continues serving reads after index failure and retries on the next call.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldRetryFailedIndexCreation()
    {
        var attempts = 0;
        indexes.CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<SubmissionDocument>>>(), token)
            .Returns(_ => ++attempts == 1
                ? Task.FromException<IEnumerable<string>>(new InvalidOperationException("Index failure"))
                : Task.FromResult<IEnumerable<string>>([]));
        var submission = CreateSubmission();
        results = [submission];

        (await repository.FindDraftAsync(submission.Cph, token)).ShouldBeSameAs(submission);
        (await repository.FindDraftAsync(submission.Cph, token)).ShouldBeSameAs(submission);
        await repository.ListAsync(submission.Cph, token);

        attempts.ShouldBe(2);
    }

    /// <summary>Converts only duplicate-key write failures to a failed insert.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldReturnFalseForDuplicateKey()
    {
        var failure = CreateWriteException(ServerErrorCategory.DuplicateKey, 11000);
        collection.InsertOneAsync(Arg.Any<SubmissionDocument>(), Arg.Any<InsertOneOptions>(), token).ThrowsAsync(failure);

        (await repository.TryInsertDraftAsync(CreateSubmission(), token)).ShouldBeFalse();
    }

    /// <summary>Preserves Mongo write failures other than duplicate keys.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldPropagateNonDuplicateWriteFailure()
    {
        var failure = CreateWriteException(ServerErrorCategory.Uncategorized, 121);
        collection.InsertOneAsync(Arg.Any<SubmissionDocument>(), Arg.Any<InsertOneOptions>(), token).ThrowsAsync(failure);

        var actual = await Should.ThrowAsync<MongoWriteException>(() => repository.TryInsertDraftAsync(CreateSubmission(), token));

        actual.ShouldBeSameAs(failure);
    }

    /// <summary>Missing legacy indexes or collections do not prevent initialization.</summary>
    /// <param name="code">The MongoDB missing namespace or index error code.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(26)]
    [InlineData(27)]
    public async Task ShouldCreateIndexesWhenLegacyIndexDoesNotExist(int code)
    {
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        var failure = new MongoCommandException(
            connection,
            "Legacy index absent",
            new BsonDocument("dropIndexes", "custom_submissions"),
            new BsonDocument { { "ok", 0 }, { "code", code } });
        indexes.DropOneAsync("unique_draft_per_cph", token).ThrowsAsync(failure);

        await repository.TryInsertDraftAsync(CreateSubmission(), token);
        await repository.ListAsync("12/345/6789", token);

        await indexes.Received(1).DropOneAsync("unique_draft_per_cph", token);
        await indexes.Received(1).CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<SubmissionDocument>>>(), token);
    }

    /// <summary>Expires only drafts at or before the exact cutoff.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldDeleteOnlyExpiredDrafts()
    {
        var cutoff = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        collection.DeleteManyAsync(Arg.Any<FilterDefinition<SubmissionDocument>>(), token)
            .Returns(new DeleteResult.Acknowledged(3));

        (await repository.DeleteExpiredDraftsAsync(cutoff, token)).ShouldBe(3);

        var expected = new BsonDocument
        {
            { "status", "DRAFT" },
            { "createdDate", new BsonDocument("$lte", new BsonDateTime(cutoff)) },
        };
        await collection.Received(1).DeleteManyAsync(
            Arg.Is<FilterDefinition<SubmissionDocument>>(filter => filter.Render(renderArgs).Equals(expected)), token);
    }

    /// <summary>Confirmed receipt deletes only the expected submitted version.</summary>
    /// <param name="count">The Mongo deletion count.</param>
    /// <param name="expected">Whether that version was deleted.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task ShouldDeleteOnlyTheConfirmedSubmittedVersion(long count, bool expected)
    {
        collection.DeleteOneAsync(Arg.Any<FilterDefinition<SubmissionDocument>>(), token)
            .Returns(new DeleteResult.Acknowledged(count));

        (await repository.DeleteSubmittedAsync("internal-id", 4, token)).ShouldBe(expected);

        var filter = new BsonDocument { { "_id", "internal-id" }, { "status", "SUBMITTED" }, { "version", 4 } };
        await collection.Received(1).DeleteOneAsync(
            Arg.Is<FilterDefinition<SubmissionDocument>>(definition => definition.Render(renderArgs).Equals(filter)), token);
    }

    /// <summary>Error expiry never deletes drafts or submitted documents.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldDeleteOnlyExpiredErrors()
    {
        var cutoff = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
        collection.DeleteManyAsync(Arg.Any<FilterDefinition<SubmissionDocument>>(), token)
            .Returns(new DeleteResult.Acknowledged(2));

        (await repository.DeleteExpiredErrorsAsync(cutoff, token)).ShouldBe(2);

        var expected = new BsonDocument
        {
            { "status", "ERROR" },
            { "createdDate", new BsonDocument("$lte", new BsonDateTime(cutoff)) },
        };
        await collection.Received(1).DeleteManyAsync(
            Arg.Is<FilterDefinition<SubmissionDocument>>(filter => filter.Render(renderArgs).Equals(expected)), token);
    }

    /// <summary>Deletes submitted records using a real submission timestamp, never creation age or missing timestamps.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldDeleteOnlyExpiredSubmittedRecordsWithTimestamps()
    {
        var cutoff = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        collection.DeleteManyAsync(Arg.Any<FilterDefinition<SubmissionDocument>>(), token)
            .Returns(new DeleteResult.Acknowledged(2));

        (await repository.DeleteExpiredSubmittedAsync(cutoff, token)).ShouldBe(2);

        var expected = new BsonDocument
        {
            { "status", "SUBMITTED" },
            { "submittedDate", new BsonDocument { { "$ne", BsonNull.Value }, { "$lte", new BsonDateTime(cutoff) } } },
        };
        await collection.Received(1).DeleteManyAsync(
            Arg.Is<FilterDefinition<SubmissionDocument>>(filter => filter.Render(renderArgs).Equals(expected)), token);
    }

    private static MongoWriteException CreateWriteException(ServerErrorCategory category, int code)
    {
        // The driver exposes WriteError but keeps its constructor internal.
        var error = (WriteError)Activator.CreateInstance(
            typeof(WriteError),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [category, code, "Write failed", new BsonDocument()],
            null)!;
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        return new MongoWriteException(connection, error, null, null);
    }

    private static SubmissionDocument CreateSubmission() => new()
    {
        Id = "submission-guid",
        HashId = "ABC-DEF-GHJ",
        Cph = "12/345/6789",
        CreatedDate = new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc),
        Status = SubmissionStatus.Draft,
    };
}
