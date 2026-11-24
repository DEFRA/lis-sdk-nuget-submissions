// <copyright file="CphTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Routing;

using Defra.Livestock.Sdk.Submissions.Routing;

/// <summary>
/// Tests for Cph.
/// </summary>
public sealed class CphTests
{
    /// <summary>
    /// Verifies: Should normalise to slashes.
    /// </summary>
    /// <param name="cph">The cph.</param>
    [Theory]
    [InlineData("12/345/6789")]
    [InlineData("12-345-6789")]
    [InlineData(" 12 / 345 / 6789 ")]
    public void ShouldNormaliseToSlashes(string cph)
    {
        Cph.Normalise(cph).ShouldBe("12/345/6789");
    }

    /// <summary>
    /// Verifies: Should reject an invalid cph.
    /// </summary>
    /// <param name="cph">The cph.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("12/345")]
    [InlineData("12/345/6789/1")]
    [InlineData("1/345/6789")]
    [InlineData("AB/345/6789")]
    public void ShouldRejectAnInvalidCph(string? cph)
    {
        Should.Throw<ArgumentException>(() => Cph.Normalise(cph));
    }
}
