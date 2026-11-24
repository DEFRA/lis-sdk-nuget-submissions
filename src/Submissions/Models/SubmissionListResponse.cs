// <copyright file="SubmissionListResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

using System.ComponentModel;

/// <summary>
/// The submissions for a CPH.
/// </summary>
public sealed class SubmissionListResponse
{
    /// <summary>Gets the submissions, newest first.</summary>
    [Description("The list of submissions for the CPH.")]
    public IReadOnlyCollection<SubmissionResponse> Submissions { get; init; } = [];
}
