// <copyright file="SubmissionOptionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>Verifies cleanup defaults and options validation.</summary>
public sealed class SubmissionOptionsTests
{
    /// <summary>Preserves existing draft retention and indefinite error retention by default.</summary>
    [Fact]
    public void ShouldDefaultToThirtyDayDraftsAndNoErrorExpiry()
    {
        var options = new SubmissionOptions();

        options.DraftExpiry.ShouldBe(TimeSpan.FromDays(30));
        options.ErrorExpiry.ShouldBeNull();
    }

    /// <summary>Rejects non-positive retention windows.</summary>
    /// <param name="draftDays">The configured draft window.</param>
    /// <param name="errorDays">The optional configured error window.</param>
    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(30, 0)]
    [InlineData(30, -1)]
    public void ShouldRejectInvalidExpiryWindows(int draftDays, int? errorDays)
    {
        var services = new ServiceCollection();
        services.AddSubmissions(options =>
        {
            options.DraftExpiry = TimeSpan.FromDays(draftDays);
            options.ErrorExpiry = errorDays is { } days ? TimeSpan.FromDays(days) : null;
        });
        using var provider = services.BuildServiceProvider();

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SubmissionOptions>>().Value);
    }
}
