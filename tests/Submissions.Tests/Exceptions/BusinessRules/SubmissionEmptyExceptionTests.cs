// <copyright file="SubmissionEmptyExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="SubmissionEmptyException"/>.
/// </summary>
public sealed class SubmissionEmptyExceptionTests
{
    /// <summary>
    /// Preserves the error details and the category used by host exception handlers.
    /// </summary>
    [Fact]
    public void ShouldDescribeFailureAndPreserveExceptionCategory()
    {
        var exception = new SubmissionEmptyException("ABC-DEF-GHJ");

        exception.ShouldBeAssignableTo<ArgumentException>();
        exception.Message.ShouldBe("Submission 'ABC-DEF-GHJ' has no animals to submit.");
    }
}
