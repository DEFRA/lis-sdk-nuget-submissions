// <copyright file="UnsupportedDataItemTypeExceptionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Tests for <see cref="UnsupportedDataItemTypeException"/>.
/// </summary>
public sealed class UnsupportedDataItemTypeExceptionTests
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
        var exception = new UnsupportedDataItemTypeException("weight", "INVALID", paramName);

        exception.ShouldBeAssignableTo<ArgumentException>();
        exception.ParamName.ShouldBe(paramName);
        exception.Message.ShouldBe(new ArgumentException("Data item 'weight' has an unsupported type 'INVALID'.", paramName).Message);
    }

    /// <summary>
    /// Describes a missing type while preserving the source parameter name.
    /// </summary>
    [Fact]
    public void ShouldDescribeMissingType()
    {
        var exception = new UnsupportedDataItemTypeException("weight", null, "itemType");

        exception.ParamName.ShouldBe("itemType");
        exception.Message.ShouldBe(new ArgumentException("Data item 'weight' has an unsupported type ''.", exception.ParamName).Message);
    }
}
