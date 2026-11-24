// <copyright file="SubmissionMongoConventions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Storage;

using Defra.Livestock.Sdk.Submissions.Models;
using MongoDB.Bson.Serialization.Conventions;

/// <summary>
/// Stores the submission documents with camel-case element names, whatever conventions the host registers, so every
/// host reads and writes the same document shape.
/// </summary>
internal static class SubmissionMongoConventions
{
    private static readonly Lock InitializationLock = new();

#pragma warning disable SA1306
    private static bool Initialized;
#pragma warning restore SA1306

    public static void Register()
    {
        lock (InitializationLock)
        {
            if (Initialized)
            {
                return;
            }

            var conventions = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
            };

            ConventionRegistry.Register(
                "Defra.Livestock.Sdk.Submissions",
                conventions,
                type => type == typeof(SubmissionDocument)
                    || type == typeof(SubmissionAnimalDocument)
                    || type == typeof(SubmissionDataItemDocument));
            Initialized = true;
        }
    }
}
