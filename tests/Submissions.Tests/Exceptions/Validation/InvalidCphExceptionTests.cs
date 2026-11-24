// <copyright file="InvalidCphExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="InvalidCphException"/>.
/// </summary>
public sealed class InvalidCphExceptionTests
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
        var exception = new InvalidCphException("Invalid holding number.", paramName);

        exception.ShouldBeAssignableTo<ArgumentException>();
        exception.ParamName.ShouldBe(paramName);
        exception.Message.ShouldBe(new ArgumentException("Invalid holding number.", paramName).Message);
    }
}
