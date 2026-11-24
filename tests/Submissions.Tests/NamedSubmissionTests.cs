// <copyright file="NamedSubmissionTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests;

using System.Net;
using System.Text;
using System.Text.Json;
using Defra.Livestock.Sdk.Core.Exceptions;
using Defra.Livestock.Sdk.Submissions.Endpoints;
using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Services;
using Defra.Livestock.Sdk.Submissions.Storage;
using Defra.Livestock.Sdk.Submissions.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;

/// <summary>Verifies independent submission installations sharing one host.</summary>
public sealed class NamedSubmissionTests
{
    /// <summary>Routes select the right storage and transfer, without duplicate endpoint names.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldIsolateRoutesDataAndTransfersInOneHost()
    {
        var register = new InMemorySubmissionRepository();
        var vetcheck = new InMemorySubmissionRepository();
        var registerTransfer = new RecordingSubmissionTransfer();
        var vetcheckTransfer = new RecordingSubmissionTransfer();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddKeyedSingleton<ISubmissionRepository>("register", register);
        builder.Services.AddKeyedSingleton<ISubmissionRepository>("vetcheck", vetcheck);
        builder.Services.AddKeyedSingleton<ISubmissionTransfer>("register", registerTransfer);
        builder.Services.AddKeyedSingleton<ISubmissionTransfer>("vetcheck", vetcheckTransfer);
        builder.Services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("register");
        builder.Services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("vetcheck");
        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (NotFoundException)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        });
        app.MapNamedSubmissionEndpoints("register", "register-submissions");
        app.MapNamedSubmissionEndpoints("vetcheck", "vetcheck-submissions");
        var token = TestContext.Current.CancellationToken;
        await app.StartAsync(token);
        using var client = app.GetTestClient();
        var registerId = await StartAsync(client, "register-submissions", token);
        var vetcheckId = await StartAsync(client, "vetcheck-submissions", token);
        register.Stored.ShouldHaveSingleItem().HashId.ShouldBe(registerId);
        vetcheck.Stored.ShouldHaveSingleItem().HashId.ShouldBe(vetcheckId);
        using var crossRead = await client.GetAsync($"/vetcheck-submissions/12/345/6789/{registerId}", token);
        crossRead.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var crossResume = await client.GetAsync($"/register-submissions/12/345/6789/start?key={vetcheckId}", token);
        crossResume.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var animal = new StringContent("{\"status\":\"complete\",\"data\":[]}", Encoding.UTF8, "application/json");
        using var saved = await client.PostAsync($"/register-submissions/12/345/6789/{registerId}/UK123", animal, token);
        saved.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var submitted = await client.PostAsync($"/register-submissions/12/345/6789/{registerId}", null, token);
        submitted.StatusCode.ShouldBe(HttpStatusCode.OK);
        registerTransfer.TransferredIds.ShouldBe([registerId]);
        vetcheckTransfer.TransferredIds.ShouldBeEmpty();
        register.Stored.ShouldBeEmpty();
        vetcheck.Stored.ShouldHaveSingleItem().HashId.ShouldBe(vetcheckId);
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray();
        var names = endpoints.Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName).ToArray();
        names.Length.ShouldBe(16);
        names.Distinct().Count().ShouldBe(16);
    }

    /// <summary>Uses different default Mongo collections and supports a named database override.</summary>
    [Fact]
    public void ShouldIsolateOptionsCollectionsAndDatabases()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var shared = Substitute.For<IMongoDatabase>();
        var dedicated = Substitute.For<IMongoDatabase>();
        services.AddSingleton(shared);
        services.AddKeyedSingleton<IMongoDatabase>("vetcheck", dedicated);
        services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("register", options =>
        {
            options.DraftExpiry = TimeSpan.FromDays(7);
            options.CollectionName = "ignored-for-named-installations";
        });
        services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("vetcheck", options => options.ErrorExpiry = TimeSpan.FromDays(60));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var options = provider.GetRequiredService<IOptionsMonitor<SubmissionOptions>>();
        options.Get("register").CollectionName.ShouldBe("register-submissions");
        options.Get("vetcheck").CollectionName.ShouldBe("vetcheck-submissions");
        options.Get("register").DraftExpiry.ShouldBe(TimeSpan.FromDays(7));
        options.Get("register").ErrorExpiry.ShouldBeNull();
        options.Get("vetcheck").DraftExpiry.ShouldBe(TimeSpan.FromDays(30));
        options.Get("vetcheck").ErrorExpiry.ShouldBe(TimeSpan.FromDays(60));
        provider.GetRequiredKeyedService<ISubmissionRepository>("register")
            .ShouldNotBeSameAs(provider.GetRequiredKeyedService<ISubmissionRepository>("vetcheck"));
        shared.Received(1).GetCollection<SubmissionDocument>("register-submissions");
        dedicated.Received(1).GetCollection<SubmissionDocument>("vetcheck-submissions");
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredKeyedService<ISubmissionService>("register")
            .ShouldNotBeSameAs(scope.ServiceProvider.GetRequiredKeyedService<ISubmissionService>("vetcheck"));
    }

    /// <summary>Each installed cleanup worker uses only its own repository and retention options.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ShouldRunCleanupForEveryInstallationWithItsOwnOptions()
    {
        var register = Substitute.For<ISubmissionRepository>();
        var vetcheck = Substitute.For<ISubmissionRepository>();
        var now = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(now));
        services.AddKeyedSingleton<ISubmissionRepository>("register", register);
        services.AddKeyedSingleton<ISubmissionRepository>("vetcheck", vetcheck);
        services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("register", options => options.DraftExpiry = TimeSpan.FromDays(7));
        services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("vetcheck", options => options.ErrorExpiry = TimeSpan.FromDays(60));
        using var provider = services.BuildServiceProvider();
        var workers = provider.GetServices<IHostedService>().OfType<SubmissionCleanupService>().ToArray();
        workers.Length.ShouldBe(2);
        var token = TestContext.Current.CancellationToken;
        foreach (var worker in workers)
        {
            await worker.CleanupAsync(token);
        }

        await register.Received(1).DeleteExpiredDraftsAsync(now.AddDays(-7).UtcDateTime, token);
        await register.DidNotReceive().DeleteExpiredErrorsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await vetcheck.Received(1).DeleteExpiredDraftsAsync(now.AddDays(-30).UtcDateTime, token);
        await vetcheck.Received(1).DeleteExpiredErrorsAsync(now.AddDays(-60).UtcDateTime, token);
    }

    /// <summary>Fails clearly rather than merging an accidentally duplicated installation.</summary>
    [Fact]
    public void ShouldRejectDuplicateInstallationNames()
    {
        var services = new ServiceCollection();
        services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("register");

        Should.Throw<ArgumentException>(() => services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("register"));
    }

    /// <summary>Named installations can coexist with the existing default registration.</summary>
    [Fact]
    public void ShouldKeepDefaultAndNamedInstallationsIndependent()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IMongoDatabase>());
        services.AddSingleton<ISubmissionTransfer, NotConfiguredSubmissionTransfer>();
        services.AddSubmissions(options => options.CollectionName = "default_submissions");
        services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("register");
        services.AddNamedSubmissions<NotConfiguredSubmissionTransfer>("vetcheck");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var defaults = scope.ServiceProvider.GetRequiredService<ISubmissionService>();
        var register = scope.ServiceProvider.GetRequiredKeyedService<ISubmissionService>("register");
        var vetcheck = scope.ServiceProvider.GetRequiredKeyedService<ISubmissionService>("vetcheck");

        defaults.ShouldNotBeSameAs(register);
        register.ShouldNotBeSameAs(vetcheck);
        scope.ServiceProvider.GetRequiredKeyedService<ISubmissionTransfer>("register")
            .ShouldNotBeSameAs(scope.ServiceProvider.GetRequiredKeyedService<ISubmissionTransfer>("vetcheck"));
        provider.GetRequiredService<IOptions<SubmissionOptions>>().Value.CollectionName.ShouldBe("default_submissions");
        provider.GetRequiredService<IOptionsMonitor<SubmissionOptions>>().Get("register").CollectionName.ShouldBe("register-submissions");
        provider.GetServices<IHostedService>().OfType<SubmissionCleanupService>().Count().ShouldBe(3);
    }

    private static async Task<string> StartAsync(HttpClient client, string prefix, CancellationToken token)
    {
        using var response = await client.GetAsync($"/{prefix}/12/345/6789/start", token);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        return body.RootElement.GetProperty("submission-id").GetString()!;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
