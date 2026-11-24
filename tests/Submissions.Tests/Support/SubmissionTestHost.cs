// <copyright file="SubmissionTestHost.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Support;

using Defra.Livestock.Sdk.Core.Exceptions;
using Defra.Livestock.Sdk.Submissions.Endpoints;
using Defra.Livestock.Sdk.Submissions.Exceptions;
using Defra.Livestock.Sdk.Submissions.Services;
using Defra.Livestock.Sdk.Submissions.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

/// <summary>
/// A minimal host for the submission endpoints, mapping exceptions to status codes the way the BE4FE hosts do.
/// </summary>
internal sealed class SubmissionTestHost : IAsyncDisposable
{
    private readonly WebApplication app;

    private SubmissionTestHost(WebApplication app, InMemorySubmissionRepository repository, RecordingSubmissionTransfer transfer)
    {
        this.app = app;
        Repository = repository;
        Transfer = transfer;
        Client = app.GetTestClient();
    }

    public InMemorySubmissionRepository Repository { get; }

    public RecordingSubmissionTransfer Transfer { get; }

    public HttpClient Client { get; }

    public static async Task<SubmissionTestHost> StartAsync(string? routePrefix = null)
    {
        var repository = new InMemorySubmissionRepository();
        var transfer = new RecordingSubmissionTransfer();

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        // The in-memory repository replaces the Mongo one, so the database is never resolved.
        builder.Services.AddSingleton<IMongoDatabase>(_ => throw new InvalidOperationException("Mongo is not used by these tests."));
        builder.Services.AddSingleton<ISubmissionRepository>(repository);
        builder.Services.AddSingleton<ISubmissionTransfer>(transfer);
        builder.Services.AddSubmissions();

        var app = builder.Build();
        app.Use(MapExceptions);
        if (routePrefix is null)
        {
            app.MapSubmissionEndpoints();
        }
        else
        {
            app.MapSubmissionEndpoints(routePrefix);
        }

        await app.StartAsync(TestContext.Current.CancellationToken);

        return new SubmissionTestHost(app, repository, transfer);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await app.DisposeAsync();
    }

    private static async Task MapExceptions(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException or SubmissionConflictException => StatusCodes.Status409Conflict,
                ArgumentException or BadHttpRequestException => StatusCodes.Status400BadRequest,
                ServiceUnavailableException => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError,
            };
        }
    }
}
