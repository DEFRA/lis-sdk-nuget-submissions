// <copyright file="SubmissionHashIdTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests;

using Defra.Livestock.Sdk.Core.Extensions;

/// <summary>
/// Tests for SubmissionHashId.
/// </summary>
public sealed class SubmissionHashIdTests
{
    /// <summary>
    /// The ids cattle-register created before the submission process moved into this package. They must not change,
    /// or stored submissions could no longer be found by the ids callers already hold.
    /// </summary>
    /// <param name="submissionGuid">The submission's GUID.</param>
    /// <param name="expected">The id cattle-register created for it.</param>
    [Theory]
    [InlineData("196ee823-db52-493e-b9c1-face71dd6be2", "PRS-PJY-WV8")]
    [InlineData("8e22ec7c-cdee-4cff-8953-ef6b4bdc936a", "X3K-WHY-YSM")]
    public void CoreDisplayHashShouldMatchTheIdsAlreadyIssued(string submissionGuid, string expected)
    {
        Guid.Parse(submissionGuid).CreateDisplayHash().ShouldBe(expected);
    }

    /// <summary>
    /// Verifies: Should normalise what a caller may have typed.
    /// </summary>
    /// <param name="received">The received.</param>
    /// <param name="expected">The expected.</param>
    [Theory]
    [InlineData("prs-pjy-wv8", "PRS-PJY-WV8")]
    [InlineData(" PRS-PJY-WV8 ", "PRS-PJY-WV8")]
    [InlineData("oO0-iI1-lL1", "000-111-111")]
    public void ShouldNormaliseWhatACallerMayHaveTyped(string received, string expected)
    {
        SubmissionHashId.Normalise(received).ShouldBe(expected);
    }
}
