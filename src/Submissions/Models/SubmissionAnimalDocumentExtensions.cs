// <copyright file="SubmissionAnimalDocumentExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

internal static class SubmissionAnimalDocumentExtensions
{
    extension(SubmissionAnimalDocument source)
    {
        public AnimalResponse ToAnimalResponse()
        {
            return new AnimalResponse
            {
                AnimalId = source.AnimalId.Trim(),
                Status = source.Status,
                Data =
                [
                    .. (source.Data ?? [])
                    .Select(item => item.ToDataItem()),
                ],
            };
        }
    }
}
