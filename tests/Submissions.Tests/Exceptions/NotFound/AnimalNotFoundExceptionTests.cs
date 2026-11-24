// <copyright file="AnimalNotFoundExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Core.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="AnimalNotFoundException"/>.
/// </summary>
public sealed class AnimalNotFoundExceptionTests
{
    /// <summary>
    /// Preserves the error details and the category used by host exception handlers.
    /// </summary>
    [Fact]
    public void ShouldDescribeFailureAndPreserveExceptionCategory()
    {
        var exception = new AnimalNotFoundException("UK123", "ABC-DEF-GHJ");

        exception.ShouldBeAssignableTo<NotFoundException>();
        exception.Message.ShouldBe("Animal 'UK123' was not found in submission 'ABC-DEF-GHJ'.");
    }
}
