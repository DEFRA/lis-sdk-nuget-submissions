// <copyright file="SubmissionTransferNotConfiguredExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Core.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="SubmissionTransferNotConfiguredException"/>.
/// </summary>
public sealed class SubmissionTransferNotConfiguredExceptionTests
{
    /// <summary>
    /// Preserves the error details and the category used by host exception handlers.
    /// </summary>
    [Fact]
    public void ShouldDescribeFailureAndPreserveExceptionCategory()
    {
        var exception = new SubmissionTransferNotConfiguredException();

        exception.ShouldBeAssignableTo<ServiceUnavailableException>();
        exception.Message.ShouldBe("Submitting is not available yet.");
    }
}
