// <copyright file="SubmissionAlreadySubmittedExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="SubmissionAlreadySubmittedException"/>.
/// </summary>
public sealed class SubmissionAlreadySubmittedExceptionTests
{
    /// <summary>
    /// Preserves the error details and the category used by host exception handlers.
    /// </summary>
    [Fact]
    public void ShouldDescribeFailureAndPreserveExceptionCategory()
    {
        var exception = new SubmissionAlreadySubmittedException("ABC-DEF-GHJ");

        exception.ShouldBeAssignableTo<SubmissionConflictException>();
        exception.Message.ShouldBe("Submission 'ABC-DEF-GHJ' has already been submitted.");
    }
}
