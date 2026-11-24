// <copyright file="AnimalIdRequiredExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="AnimalIdRequiredException"/>.
/// </summary>
public sealed class AnimalIdRequiredExceptionTests
{
    /// <summary>
    /// Preserves the source parameter name and includes it in the argument error message.
    /// </summary>
    /// <param name="paramName">The parameter name supplied by the caller.</param>
    [Theory]
    [InlineData("source")]
    [InlineData("customParameter")]
    [InlineData("")]
    public void ShouldForwardParameterNameAndDescribeFailure(string paramName)
    {
        var exception = new AnimalIdRequiredException(paramName);

        exception.ShouldBeAssignableTo<ArgumentException>();
        exception.ParamName.ShouldBe(paramName);
        exception.Message.ShouldBe(new ArgumentException("An animal id is required.", paramName).Message);
    }
}
