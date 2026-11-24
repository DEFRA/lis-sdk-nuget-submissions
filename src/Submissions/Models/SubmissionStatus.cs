// <copyright file="SubmissionStatus.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Models;

/// <summary>
/// The statuses a submission can have.
/// </summary>
public static class SubmissionStatus
{
    /// <summary>The submission can still be changed.</summary>
    public const string Draft = "DRAFT";

    /// <summary>The submission has been transferred and can no longer be changed.</summary>
    public const string Submitted = "SUBMITTED";

    /// <summary>The submission failed and is retained for investigation.</summary>
    public const string Error = "ERROR";
}
