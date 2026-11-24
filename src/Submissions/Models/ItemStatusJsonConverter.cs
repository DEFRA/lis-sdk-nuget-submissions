// <copyright file="ItemStatusJsonConverter.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serializes animal statuses as strings and rejects integer representations.
/// </summary>
internal sealed class ItemStatusJsonConverter()
    : JsonStringEnumConverter<ItemStatus>(
        JsonNamingPolicy.CamelCase,
        allowIntegerValues: false);
