// <copyright file="SubmissionOptions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions;

/// <summary>
/// Options for the submission process.
/// </summary>
public sealed class SubmissionOptions
{
    /// <summary>
    /// Gets or sets the MongoDB collection for the default installation. Defaults to <c>submissions</c>.
    /// Named installations derive their collection as <c>{name}-submissions</c> and ignore this setting.
    /// </summary>
    public string CollectionName { get; set; } = "submissions";

    /// <summary>
    /// Gets or sets the maximum draft age from creation. Defaults to 30 days and must be positive.
    /// </summary>
    public TimeSpan DraftExpiry { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets the submitted retention window from the submission timestamp. Defaults to 30 days and must be positive.
    /// </summary>
    public TimeSpan SubmittedExpiry { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets the maximum error submission age from creation. Defaults to 30 days and must be positive.
    /// </summary>
    public TimeSpan ErrorExpiry { get; set; } = TimeSpan.FromDays(30);
}
