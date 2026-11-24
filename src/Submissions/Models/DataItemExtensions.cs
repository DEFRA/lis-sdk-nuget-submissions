// <copyright file="DataItemExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using Defra.Livestock.Sdk.Submissions.Exceptions;

internal static class DataItemExtensions
{
    extension(DataItem source)
    {
        public SubmissionDataItemDocument ToSubmissionDataItemDocument()
        {
            if (string.IsNullOrWhiteSpace(source.Key))
            {
                throw new DataItemKeyRequiredException(nameof(source.Key));
            }

            var type = source.Type.Trim().ToUpperInvariant();
            if (!PropertyTypes.IsSupported(type))
            {
                throw new UnsupportedDataItemTypeException(source.Key, source.Type, nameof(source.Type));
            }

            return new SubmissionDataItemDocument
            {
                Key = source.Key,
                Value = source.Value,
                Type = type,
            };
        }
    }
}
