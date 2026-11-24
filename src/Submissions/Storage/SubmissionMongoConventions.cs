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
#pragma warning disable SA1306
    private static int Initialized;
#pragma warning restore SA1306

    public static void Register()
    {
        if (Interlocked.Exchange(ref Initialized, 1) == 1)
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
            type => type.Namespace == typeof(SubmissionDocument).Namespace);
    }
}
