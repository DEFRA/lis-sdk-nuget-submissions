// <copyright file="SubmissionEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Endpoints;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Routing;
using Defra.Livestock.Sdk.Submissions.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The BFF framework endpoints for submission. They sit at the <c>/submission</c> root the framework sets out,
/// rather than under <c>/api</c>.
/// </summary>
public static class SubmissionEndpoints
{
    /// <summary>
    /// The default route prefix of the submission endpoints.
    /// </summary>
    public const string DefaultRoutePrefix = "submission";

    /// <param name="app">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the submission endpoints. The host adds its own authorization and other conventions to the group returned,
        /// for example <c>app.MapSubmissionEndpoints().RequireAuthorization(policy)</c>.
        /// </summary>
        /// <param name="routePrefix">The route prefix, <c>submission</c> unless given.</param>
        /// <returns>The route group the endpoints are mapped to.</returns>
        [SuppressMessage(
            "S1192",
            "S1192:String literals should not be duplicated",
            Justification = "I want the routes to be fully written.")]
        public RouteGroupBuilder MapSubmissionEndpoints(string routePrefix = DefaultRoutePrefix)
        {
            return MapEndpoints(app, routePrefix, null);
        }

        /// <summary>Maps routes to a named installation with unique endpoint names.</summary>
        /// <param name="name">The name passed to AddNamedSubmissions.</param>
        /// <param name="routePrefix">The installation's route prefix.</param>
        /// <returns>The route group for host authorization and conventions.</returns>
        public RouteGroupBuilder MapNamedSubmissionEndpoints(string name, string routePrefix)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);
            return MapEndpoints(app, routePrefix, name);
        }
    }

    [SuppressMessage(
        "S1192",
        "S1192:String literals should not be duplicated",
        Justification = "The routes are intentionally written in full.")]
    private static RouteGroupBuilder MapEndpoints(IEndpointRouteBuilder app, string routePrefix, string? name)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app
            .MapGroup("/" + routePrefix.Trim('/'))
            .WithTags(name ?? OpenApiMetadata.Tag);

        group.MapGet("/{county}/{parish}/{holding}", ([AsParameters] CphRoute cph, HttpContext context, CancellationToken token) => ListSubmissions(cph, ResolveService(context, name), token))
            .WithName(name is null ? OpenApiMetadata.ListSubmissions.Name : name + "." + OpenApiMetadata.ListSubmissions.Name)
            .WithSummary(OpenApiMetadata.ListSubmissions.Summary)
            .WithDescription(OpenApiMetadata.ListSubmissions.Description)
            .Produces<SubmissionListResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{county}/{parish}/{holding}/start", ([AsParameters] CphRoute cph, string? key, HttpContext context, CancellationToken token) => StartSubmission(cph, key, ResolveService(context, name), token))
            .WithName(name is null ? OpenApiMetadata.StartSubmission.Name : name + "." + OpenApiMetadata.StartSubmission.Name)
            .WithSummary(OpenApiMetadata.StartSubmission.Summary)
            .WithDescription(OpenApiMetadata.StartSubmission.Description)
            .Produces<SubmissionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{county}/{parish}/{holding}/{submissionId}", ([AsParameters] CphRoute cph, string submissionId, HttpContext context, CancellationToken token) => GetSubmission(cph, submissionId, ResolveService(context, name), token))
            .WithName(name is null ? OpenApiMetadata.GetSubmission.Name : name + "." + OpenApiMetadata.GetSubmission.Name)
            .WithSummary(OpenApiMetadata.GetSubmission.Summary)
            .WithDescription(OpenApiMetadata.GetSubmission.Description)
            .Produces<SubmissionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{county}/{parish}/{holding}/{submissionId}/{animalId}", ([AsParameters] CphRoute cph, string submissionId, string animalId, HttpContext context, CancellationToken token) => GetSubmissionAnimal(cph, submissionId, animalId, ResolveService(context, name), token))
            .WithName(name is null ? OpenApiMetadata.GetAnimal.Name : name + "." + OpenApiMetadata.GetAnimal.Name)
            .WithSummary(OpenApiMetadata.GetAnimal.Summary)
            .WithDescription(OpenApiMetadata.GetAnimal.Description)
            .Produces<AnimalResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{county}/{parish}/{holding}/{submissionId}/{animalId}", ([AsParameters] CphRoute cph, string submissionId, string animalId, AnimalRequest? request, HttpContext context, CancellationToken token) => UpsertSubmissionAnimal(cph, submissionId, animalId, request, ResolveService(context, name), token))
            .WithName(name is null ? OpenApiMetadata.UpsertAnimal.Name : name + "." + OpenApiMetadata.UpsertAnimal.Name)
            .WithSummary(OpenApiMetadata.UpsertAnimal.Summary)
            .WithDescription(OpenApiMetadata.UpsertAnimal.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{county}/{parish}/{holding}/{submissionId}/{animalId}", ([AsParameters] CphRoute cph, string submissionId, string animalId, AnimalRequest? request, HttpContext context, CancellationToken token) => UpsertSubmissionAnimal(cph, submissionId, animalId, request, ResolveService(context, name), token))
            .WithName(name is null ? OpenApiMetadata.PatchAnimal.Name : name + "." + OpenApiMetadata.PatchAnimal.Name)
            .WithSummary(OpenApiMetadata.PatchAnimal.Summary)
            .WithDescription(OpenApiMetadata.PatchAnimal.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{county}/{parish}/{holding}/{submissionId}/{animalId}", ([AsParameters] CphRoute cph, string submissionId, string animalId, HttpContext context, CancellationToken token) => DeleteSubmissionAnimal(cph, submissionId, animalId, ResolveService(context, name), token))
            .WithName(name is null ? OpenApiMetadata.DeleteAnimal.Name : name + "." + OpenApiMetadata.DeleteAnimal.Name)
            .WithSummary(OpenApiMetadata.DeleteAnimal.Summary)
            .WithDescription(OpenApiMetadata.DeleteAnimal.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{county}/{parish}/{holding}/{submissionId}", ([AsParameters] CphRoute cph, string submissionId, HttpContext context, CancellationToken token) => SubmitSubmission(cph, submissionId, ResolveService(context, name), token))
            .WithName(name is null ? OpenApiMetadata.SubmitSubmission.Name : name + "." + OpenApiMetadata.SubmitSubmission.Name)
            .WithSummary(OpenApiMetadata.SubmitSubmission.Summary)
            .WithDescription(OpenApiMetadata.SubmitSubmission.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return group;
    }

    private static ISubmissionService ResolveService(HttpContext context, string? name) => name is null
        ? context.RequestServices.GetRequiredService<ISubmissionService>()
        : context.RequestServices.GetRequiredKeyedService<ISubmissionService>(name);

    private static async Task<IResult> ListSubmissions(
        [AsParameters] CphRoute cph,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await submissionService.ListAsync(
            cph.ToString(),
            cancellationToken));
    }

    private static async Task<IResult> StartSubmission(
        [AsParameters] CphRoute cph,
        string? key,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await submissionService.StartAsync(
            cph.ToString(),
            key,
            cancellationToken));
    }

    private static async Task<IResult> GetSubmission(
        [AsParameters] CphRoute cph,
        string submissionId,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await submissionService.GetSubmissionAsync(
            cph.ToString(),
            submissionId,
            cancellationToken));
    }

    private static async Task<IResult> GetSubmissionAnimal(
        [AsParameters] CphRoute cph,
        string submissionId,
        string animalId,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await submissionService.GetAnimalAsync(
            cph.ToString(),
            submissionId,
            animalId,
            cancellationToken));
    }

    private static async Task<IResult> UpsertSubmissionAnimal(
        [AsParameters] CphRoute cph,
        string submissionId,
        string animalId,
        AnimalRequest? request,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await submissionService.UpsertAnimalAsync(
            cph.ToString(),
            submissionId,
            animalId,
            request,
            cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<IResult> DeleteSubmissionAnimal(
        [AsParameters] CphRoute cph,
        string submissionId,
        string animalId,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        await submissionService.DeleteAnimalAsync(
            cph.ToString(),
            submissionId,
            animalId,
            cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<IResult> SubmitSubmission(
        [AsParameters] CphRoute cph,
        string submissionId,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        await submissionService.SubmitAsync(
            cph.ToString(),
            submissionId,
            cancellationToken);
        return TypedResults.Ok();
    }
}
