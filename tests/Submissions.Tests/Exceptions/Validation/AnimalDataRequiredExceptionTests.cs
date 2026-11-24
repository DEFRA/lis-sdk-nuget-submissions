// <copyright file="AnimalDataRequiredExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="AnimalDataRequiredException"/>.
/// </summary>
public sealed class AnimalDataRequiredExceptionTests
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
        var exception = new AnimalDataRequiredException(paramName);

        exception.ShouldBeAssignableTo<ArgumentException>();
        exception.ParamName.ShouldBe(paramName);
        exception.Message.ShouldBe(new ArgumentException("Data must be an array of key, value and type items.", paramName).Message);
    }
}
