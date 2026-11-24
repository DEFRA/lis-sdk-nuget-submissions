// <copyright file="SubmissionChangedExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="SubmissionChangedException"/>.
/// </summary>
public sealed class SubmissionChangedExceptionTests
{
    /// <summary>
    /// Preserves the error details and the category used by host exception handlers.
    /// </summary>
    [Fact]
    public void ShouldDescribeFailureAndPreserveExceptionCategory()
    {
        var exception = new SubmissionChangedException("ABC-DEF-GHJ");

        exception.ShouldBeAssignableTo<SubmissionConflictException>();
        exception.Message.ShouldBe("Submission 'ABC-DEF-GHJ' was changed by another request. Reload it and try again.");
    }
}
