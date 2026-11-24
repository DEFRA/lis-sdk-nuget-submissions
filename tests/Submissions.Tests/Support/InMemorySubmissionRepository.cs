// <copyright file="InMemorySubmissionRepository.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Support;

using System.Collections.Concurrent;
using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

/// <summary>
/// Holds submissions as BSON in memory, so tests also cover the documents' serialisation, but not MongoDB itself.
/// </summary>
internal sealed class InMemorySubmissionRepository : ISubmissionRepository
{
    private readonly ConcurrentDictionary<string, byte[]> stored = new();

    public SubmissionDocument? DraftCreatedByAnotherRequest { get; set; }

    public int HashIdClashes { get; set; }

    public List<SubmissionDocument> Stored => All();

    public bool RejectNextReplace { get; set; }

    public Task<SubmissionDocument?> FindDraftAsync(string cph, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(All().Where(s => s.Cph == cph && s.Status == SubmissionStatus.Draft)
            .OrderByDescending(s => s.CreatedDate).FirstOrDefault());
    }

    public Task<bool> TryInsertDraftAsync(SubmissionDocument submission, CancellationToken cancellationToken = default)
    {
        if (DraftCreatedByAnotherRequest is { } winner)
        {
            DraftCreatedByAnotherRequest = null;
            stored[winner.Id] = winner.ToBson();
            return Task.FromResult(false);
        }

        if (HashIdClashes > 0)
        {
            HashIdClashes--;
            return Task.FromResult(false);
        }

        stored[submission.Id] = submission.ToBson();
        return Task.FromResult(true);
    }

    public Task<SubmissionDocument?> GetByHashIdAsync(string hashId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(All().FirstOrDefault(s => s.HashId == hashId));
    }

    public Task<IReadOnlyCollection<SubmissionDocument>> ListAsync(string cph, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<SubmissionDocument> submissions = All()
            .Where(s => s.Cph == cph)
            .OrderByDescending(s => s.CreatedDate)
            .ToList();
        return Task.FromResult(submissions);
    }

    public Task<bool> TryReplaceDraftAsync(SubmissionDocument submission, int expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectNextReplace)
        {
            RejectNextReplace = false;
            return Task.FromResult(false);
        }

        var current = BsonSerializer.Deserialize<SubmissionDocument>(stored[submission.Id]);
        if (current.Status != SubmissionStatus.Draft || current.Version != expectedVersion)
        {
            return Task.FromResult(false);
        }

        stored[submission.Id] = submission.ToBson();
        return Task.FromResult(true);
    }

    public Task<long> DeleteExpiredDraftsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        long deleted = 0;
        foreach (var submission in All().Where(s => s.Status == SubmissionStatus.Draft && s.CreatedDate <= cutoffUtc))
        {
            if (stored.TryRemove(submission.Id, out _))
            {
                deleted++;
            }
        }

        return Task.FromResult(deleted);
    }

    public Task<long> DeleteExpiredErrorsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        long deleted = 0;
        foreach (var submission in All().Where(s => s.Status == SubmissionStatus.Error && s.CreatedDate <= cutoffUtc))
        {
            if (stored.TryRemove(submission.Id, out _))
            {
                deleted++;
            }
        }

        return Task.FromResult(deleted);
    }

    public Task<bool> DeleteSubmittedAsync(string id, int expectedVersion, CancellationToken cancellationToken = default)
    {
        if (!stored.TryGetValue(id, out var bytes))
        {
            return Task.FromResult(false);
        }

        var submission = BsonSerializer.Deserialize<SubmissionDocument>(bytes);
        return Task.FromResult(submission.Status == SubmissionStatus.Submitted
            && submission.Version == expectedVersion && stored.TryRemove(id, out _));
    }

    private List<SubmissionDocument> All() =>
        stored.Values.Select(bytes => BsonSerializer.Deserialize<SubmissionDocument>(bytes)).ToList();
}
