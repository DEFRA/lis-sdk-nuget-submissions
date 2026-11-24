// <copyright file="DraftSubmissionCreationExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="DraftSubmissionCreationException"/>.
/// </summary>
public sealed class DraftSubmissionCreationExceptionTests
{
    /// <summary>
    /// Preserves the error details and the category used by host exception handlers.
    /// </summary>
    [Fact]
    public void ShouldDescribeFailureAndPreserveExceptionCategory()
    {
        var exception = new DraftSubmissionCreationException("12/345/6789");

        exception.ShouldBeAssignableTo<SubmissionConflictException>();
        exception.Message.ShouldBe("A draft submission for CPH '12/345/6789' could not be started. Try again.");
    }
}
