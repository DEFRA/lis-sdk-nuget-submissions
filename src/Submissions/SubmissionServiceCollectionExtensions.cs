// <copyright file="SubmissionServiceCollectionExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions;

using Defra.Livestock.Sdk.Submissions.Services;
using Defra.Livestock.Sdk.Submissions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

/// <summary>
/// Registers the submission process.
/// </summary>
public static class SubmissionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the submission repository and service. The host must also register an <c>IMongoDatabase</c> and an
    /// <see cref="ISubmissionTransfer"/>, such as <see cref="NotConfiguredSubmissionTransfer"/> until its transfer is built.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Changes the <see cref="SubmissionOptions"/>, for example the collection name.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    public static IServiceCollection AddSubmissions(this IServiceCollection services, Action<SubmissionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        SubmissionMongoConventions.Register();

        var options = services.AddOptions<SubmissionOptions>()
            .Validate(value => value.DraftExpiry > TimeSpan.Zero, "DraftExpiry must be positive.")
            .Validate(value => value.SubmittedExpiry > TimeSpan.Zero, "SubmittedExpiry must be positive.")
            .Validate(value => value.ErrorExpiry > TimeSpan.Zero, "ErrorExpiry must be positive.")
            .ValidateOnStart();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ISubmissionRepository, MongoSubmissionRepository>();
        services.TryAddScoped<ISubmissionService, SubmissionService>();
        services.AddHostedService<SubmissionCleanupService>();

        return services;
    }

    /// <summary>Registers an isolated named installation with its own transfer, options, storage and cleanup.</summary>
    /// <typeparam name="TTransfer">The transfer implementation for this installation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The installation name, also used as its dependency injection key.</param>
    /// <param name="configure">Configures retention for this installation. Its collection is derived as name-submissions.</param>
    /// <returns>The same service collection for chaining further registrations.</returns>
    public static IServiceCollection AddNamedSubmissions<TTransfer>(
        this IServiceCollection services,
        string name,
        Action<SubmissionOptions>? configure = null)
        where TTransfer : class, ISubmissionTransfer
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(ISubmissionService)
            && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, name)))
        {
            throw new ArgumentException("A submission installation with this name is already registered.", nameof(name));
        }

        SubmissionMongoConventions.Register();
        var options = services.AddOptions<SubmissionOptions>(name)
            .PostConfigure(value => value.CollectionName = name + "-submissions")
            .Validate(value => value.DraftExpiry > TimeSpan.Zero, "DraftExpiry must be positive.")
            .Validate(value => value.SubmittedExpiry > TimeSpan.Zero, "SubmittedExpiry must be positive.")
            .Validate(value => value.ErrorExpiry > TimeSpan.Zero, "ErrorExpiry must be positive when configured.")
            .ValidateOnStart();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddKeyedScoped<ISubmissionTransfer, TTransfer>(name);
        services.TryAddKeyedSingleton<ISubmissionRepository>(name, (provider, _) => new MongoSubmissionRepository(
            provider.GetKeyedService<IMongoDatabase>(name) ?? provider.GetRequiredService<IMongoDatabase>(),
            Options.Create(provider.GetRequiredService<IOptionsMonitor<SubmissionOptions>>().Get(name)),
            provider.GetRequiredService<ILogger<MongoSubmissionRepository>>()));
        services.TryAddKeyedScoped<ISubmissionService>(name, (provider, _) => new SubmissionService(
            provider.GetRequiredKeyedService<ISubmissionRepository>(name),
            provider.GetRequiredKeyedService<ISubmissionTransfer>(name),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ILogger<SubmissionService>>()));
        services.AddSingleton<IHostedService>(provider => new SubmissionCleanupService(
            provider.GetRequiredKeyedService<ISubmissionRepository>(name),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ILogger<SubmissionCleanupService>>(),
            Options.Create(provider.GetRequiredService<IOptionsMonitor<SubmissionOptions>>().Get(name))));

        return services;
    }
}
