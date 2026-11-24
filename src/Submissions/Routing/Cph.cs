// <copyright file="Cph.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Routing;

using System.Text.RegularExpressions;
using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Validates and normalises a county/parish/holding number.
/// </summary>
internal static partial class Cph
{
    private static readonly char[] Separators = ['/', '-'];

    /// <summary>
    /// Validates a CPH and returns it with slashes.
    /// </summary>
    /// <param name="cph">The CPH, with slashes or dashes.</param>
    /// <exception cref="InvalidCphException">The CPH is missing or not in the format 12/345/6789.</exception>
    /// <returns>The CPH as <c>12/345/6789</c>, whether it was given with slashes or dashes.</returns>
    public static string Normalise(string? cph)
    {
        if (string.IsNullOrWhiteSpace(cph))
        {
            throw new InvalidCphException("A cph query parameter is required.", nameof(cph));
        }

        var parts = cph.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            throw new InvalidCphException("CPH must contain county, parish and holding segments.", nameof(cph));
        }

        var normalised = $"{parts[0]}/{parts[1]}/{parts[2]}";

        return CphFormat().IsMatch(normalised)
            ? normalised
            : throw new InvalidCphException("The cph query parameter must be in the format 12/345/6789.", nameof(cph));
    }

    [GeneratedRegex(@"^\d{2}/\d{3}/\d{4}$")]
    private static partial Regex CphFormat();
}
