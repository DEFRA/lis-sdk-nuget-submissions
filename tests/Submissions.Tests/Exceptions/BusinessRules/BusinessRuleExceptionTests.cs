// <copyright file="BusinessRuleExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="BusinessRuleException"/>.
/// </summary>
public sealed class BusinessRuleExceptionTests
{
    /// <summary>
    /// Preserves the error details and the category used by host exception handlers.
    /// </summary>
    [Fact]
    public void ShouldDescribeFailureAndPreserveExceptionCategory()
    {
        var exception = new BusinessRuleException("A business rule failed.");

        exception.ShouldBeAssignableTo<Exception>();
        exception.Message.ShouldBe("A business rule failed.");
    }
}
