// <copyright file="AnimalRequestExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using Defra.Livestock.Sdk.Submissions.Exceptions;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2208:Instantiate argument exceptions correctly",
    Justification = "The parameter name is part of the error detail callers already receive.")]
internal static class AnimalRequestExtensions
{
    extension(AnimalRequest source)
    {
        public SubmissionAnimalDocument ToSubmissionAnimalDocument(string animalId)
        {
            var data = new List<SubmissionDataItemDocument>(source.Data.Length);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in source.Data)
            {
                if (item is null)
                {
                    throw new NullDataItemException(nameof(source.Data));
                }

                var stored = item.ToSubmissionDataItemDocument();
                if (!keys.Add(stored.Key))
                {
                    throw new DuplicateDataItemException(stored.Key, nameof(source.Data));
                }

                data.Add(stored);
            }

            return new SubmissionAnimalDocument
            {
                AnimalId = animalId,
                Status = source.Status,
                Data = data,
            };
        }
    }
}
