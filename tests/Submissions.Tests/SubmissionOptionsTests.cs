// <copyright file="SubmissionOptionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>Verifies cleanup defaults and options validation.</summary>
public sealed class SubmissionOptionsTests
{
    /// <summary>Verify that the defaults are set.</summary>
    [Fact]
    public void ShouldDefaultToThirtyDay()
    {
        var options = new SubmissionOptions();

        options.DraftExpiry.ShouldBe(TimeSpan.FromDays(30));
        options.SubmittedExpiry.ShouldBe(TimeSpan.FromDays(30));
        options.ErrorExpiry.ShouldBe(TimeSpan.FromDays(30));
    }

    /// <summary>Rejects non-positive retention windows.</summary>
    /// <param name="draftDays">The configured draft window.</param>
    /// <param name="errorDays">The configured error window.</param>
    [Theory]
    [InlineData(0, 30)]
    [InlineData(-1, 30)]
    [InlineData(30, 0)]
    [InlineData(30, -1)]
    public void ShouldRejectInvalidExpiryWindows(int draftDays, int errorDays)
    {
        var services = new ServiceCollection();
        services.AddSubmissions(options =>
        {
            options.DraftExpiry = TimeSpan.FromDays(draftDays);
            options.ErrorExpiry = TimeSpan.FromDays(errorDays);
        });
        using var provider = services.BuildServiceProvider();

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SubmissionOptions>>().Value);
    }

    /// <summary>Rejects invalid submitted retention rather than deleting records immediately.</summary>
    /// <param name="days">The submitted retention window.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldRejectInvalidSubmittedExpiry(int days)
    {
        var services = new ServiceCollection();
        services.AddSubmissions(options => options.SubmittedExpiry = TimeSpan.FromDays(days));
        using var provider = services.BuildServiceProvider();

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SubmissionOptions>>().Value);
    }

    /// <summary>Accepts valid retention windows for all submission statuses.</summary>
    [Fact]
    public void ShouldAcceptPositiveExpiryWindows()
    {
        var services = new ServiceCollection();
        services.AddSubmissions(options =>
        {
            options.DraftExpiry = TimeSpan.FromDays(7);
            options.SubmittedExpiry = TimeSpan.FromDays(30);
            options.ErrorExpiry = TimeSpan.FromDays(60);
        });
        using var provider = services.BuildServiceProvider();

        var configured = provider.GetRequiredService<IOptions<SubmissionOptions>>().Value;

        configured.DraftExpiry.ShouldBe(TimeSpan.FromDays(7));
        configured.SubmittedExpiry.ShouldBe(TimeSpan.FromDays(30));
        configured.ErrorExpiry.ShouldBe(TimeSpan.FromDays(60));
    }
}
