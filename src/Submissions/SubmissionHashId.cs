// <copyright file="SubmissionHashId.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions;

/// <summary>
/// The id callers use for a submission: a display hash of its GUID, as <c>XXX-XXX-XXX</c>.
/// </summary>
internal static class SubmissionHashId
{
    /// <summary>
    /// Returns the stored form of an id received from a caller, who may have typed it in lower case or mistaken a
    /// digit for a similar letter. As Crockford base32 sets out, <c>O</c> is read as <c>0</c>, and <c>I</c> and <c>L</c> as <c>1</c>.
    /// </summary>
    /// <param name="id">The id received.</param>
    /// <returns>The id in upper case, without surrounding white space or look-alike letters.</returns>
    public static string Normalise(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return id.Trim()
            .ToUpperInvariant()
            .Replace('O', '0')
            .Replace('I', '1')
            .Replace('L', '1');
    }
}
