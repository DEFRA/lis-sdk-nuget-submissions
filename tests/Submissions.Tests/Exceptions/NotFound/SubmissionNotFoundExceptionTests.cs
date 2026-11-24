// <copyright file="SubmissionNotFoundExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Core.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="SubmissionNotFoundException"/>.
/// </summary>
public sealed class SubmissionNotFoundExceptionTests
{
    /// <summary>
    /// Preserves the error details and the category used by host exception handlers.
    /// </summary>
    [Fact]
    public void ShouldDescribeFailureAndPreserveExceptionCategory()
    {
        var exception = new SubmissionNotFoundException("ABC-DEF-GHJ");

        exception.ShouldBeAssignableTo<NotFoundException>();
        exception.Message.ShouldBe("Submission 'ABC-DEF-GHJ' was not found.");
    }
}
