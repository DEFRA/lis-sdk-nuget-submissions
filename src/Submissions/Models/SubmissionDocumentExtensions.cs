// <copyright file="SubmissionDocumentExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

internal static class SubmissionDocumentExtensions
{
    extension(SubmissionDocument source)
    {
        public SubmissionTransferRequest ToSubmissionTransferRequest()
        {
            return new SubmissionTransferRequest
            {
                SubmissionId = source.HashId,
                Cph = source.Cph,
                CreatedDate = ToUtcOffset(source.CreatedDate),
                Animals = source.Animals.Select(animal => animal.ToAnimalResponse()).ToArray(),
            };
        }

        public SubmissionResponse ToSubmissionResponse()
        {
            return new SubmissionResponse
            {
                SubmissionId = source.HashId,
                Status = source.Status,
                CreatedDate = ToUtcOffset(source.CreatedDate),
                AnimalCount = source.Animals.Count,
                Animals = source.ToAnimalSummaries(),
                SubmittedDate = ToUtcOffset(source.SubmittedDate),
            };
        }

        public List<SubmissionAnimalSummary> ToAnimalSummaries()
        {
            return
            [
                .. source
                    .Animals
                    .Select(animal => new SubmissionAnimalSummary
                    {
                        AnimalId = animal.AnimalId,
                        Status = animal.Status,
                    }),
            ];
        }
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? ToUtcOffset(DateTime? value) =>
        value.HasValue ? ToUtcOffset(value.Value) : null;
}
