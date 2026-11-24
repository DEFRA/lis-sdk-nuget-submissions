// <copyright file="MongoSubmissionRepository.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Storage;

using Defra.Livestock.Sdk.Submissions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

/// <summary>
/// Stores submissions in a MongoDB collection, creating its indexes on first use.
/// </summary>
internal sealed partial class MongoSubmissionRepository : ISubmissionRepository
{
    private const int IndexesNotCreated = 0;
    private const int IndexesBeingCreated = 1;
    private const int IndexesCreated = 2;
    private readonly IMongoCollection<SubmissionDocument> collection;
    private readonly ILogger<MongoSubmissionRepository> logger;
    private int indexState = IndexesNotCreated;

    public MongoSubmissionRepository(
        IMongoDatabase database,
        IOptions<SubmissionOptions> options,
        ILogger<MongoSubmissionRepository> logger)
    {
        collection = database.GetCollection<SubmissionDocument>(options.Value.CollectionName);
        this.logger = logger;
    }

    public async Task<SubmissionDocument?> FindDraftAsync(string cph, CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        return await collection
            .Find(submission => submission.Cph == cph && submission.Status == SubmissionStatus.Draft)
            .SortByDescending(submission => submission.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryInsertDraftAsync(SubmissionDocument submission, CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        try
        {
            await collection.InsertOneAsync(submission, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task<SubmissionDocument?> GetByHashIdAsync(string hashId, CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        return await collection
            .Find(submission => submission.HashId == hashId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SubmissionDocument>> ListAsync(string cph, CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        return await collection
            .Find(submission => submission.Cph == cph)
            .SortByDescending(submission => submission.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryReplaceDraftAsync(SubmissionDocument submission, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var result = await collection.ReplaceOneAsync(
            stored => stored.Id == submission.Id
                && stored.Status == SubmissionStatus.Draft
                && stored.Version == expectedVersion,
            submission,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    public async Task<long> DeleteExpiredDraftsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        var result = await collection.DeleteManyAsync(
            submission => submission.Status == SubmissionStatus.Draft && submission.CreatedDate <= cutoffUtc,
            cancellationToken);
        return result.DeletedCount;
    }

    public async Task<long> DeleteExpiredErrorsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        var result = await collection.DeleteManyAsync(
            submission => submission.Status == SubmissionStatus.Error && submission.CreatedDate <= cutoffUtc,
            cancellationToken);
        return result.DeletedCount;
    }

    public async Task<bool> DeleteSubmittedAsync(string id, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var result = await collection.DeleteOneAsync(
            submission => submission.Id == id
                && submission.Status == SubmissionStatus.Submitted
                && submission.Version == expectedVersion,
            cancellationToken);
        return result.DeletedCount == 1;
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        // Same approach as the cache repository: one caller creates the indexes, and a failure is retried later.
        if (Interlocked.CompareExchange(ref indexState, IndexesBeingCreated, IndexesNotCreated) != IndexesNotCreated)
        {
            return;
        }

        var keys = Builders<SubmissionDocument>.IndexKeys;

        var uniqueHashId = new CreateIndexModel<SubmissionDocument>(
            keys.Ascending(submission => submission.HashId),
            new CreateIndexOptions { Name = "unique_hashId", Unique = true });

        var listByCph = new CreateIndexModel<SubmissionDocument>(
            keys.Ascending(submission => submission.Cph).Descending(submission => submission.CreatedDate),
            new CreateIndexOptions { Name = "cph_createdDate" });

        try
        {
            // Remove the old constraint from collections created by earlier versions.
            try
            {
                await collection.Indexes.DropOneAsync("unique_draft_per_cph", cancellationToken);
            }
            catch (MongoCommandException ex) when (ex.Code is 26 or 27)
            {
                // A new collection or an already removed index needs no migration.
            }

            await collection.Indexes.CreateManyAsync([uniqueHashId, listByCph], cancellationToken);
            Volatile.Write(ref indexState, IndexesCreated);
        }
        catch (Exception ex)
        {
            Volatile.Write(ref indexState, IndexesNotCreated);
            LogIndexCreationFailed(ex);
        }
    }
}
