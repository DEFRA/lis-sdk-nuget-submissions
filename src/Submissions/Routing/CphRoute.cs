// <copyright file="CphRoute.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Routing;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The county, parish and holding route segments of a submission endpoint.
/// </summary>
internal sealed class CphRoute
{
    [FromRoute(Name = "county")]
    public required string County { get; init; }

    [FromRoute(Name = "parish")]
    public required string Parish { get; init; }

    [FromRoute(Name = "holding")]
    public required string Holding { get; init; }

    public override string ToString() => $"{County}/{Parish}/{Holding}";
}
