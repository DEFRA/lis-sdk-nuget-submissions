# lis-sdk-nuget-submissions

Part of the Livestock Information Service (LIS), which lets livestock keepers, owners, agents and
delegates record and manage their animals.

`Defra.Livestock.Sdk.Submissions` is the submission process shared by the LIS BE4FE services: draft submissions per
CPH stored in MongoDB, the animals and typed data items in it, the BFF framework `/submission` endpoints, and the hook
that transfers a completed submission downstream.

Used by: [be4fe-cattle-register](https://github.com/DEFRA/lis-be4fe-cattle-register) (birth) and
[be4fe-cattle-death](https://github.com/DEFRA/lis-be4fe-cattle-death) (death).

## Installation

```bash
dotnet add package Defra.Livestock.Sdk.Submissions
```

The package targets `net10.0` and depends on `Defra.Livestock.Sdk.Core`, `MongoDB.Driver` and the ASP.NET Core shared
framework.

## Usage

```csharp
using Defra.Livestock.Sdk.Submissions;
using Defra.Livestock.Sdk.Submissions.Endpoints;
using Defra.Livestock.Sdk.Submissions.Services;

// The host owns its Mongo connection and supplies the database.
services.AddSingleton(provider => provider.GetRequiredService<IMongoDbClientFactory>().GetDatabase());

services.AddSubmissions();                                                      // repository, service, options
services.AddSingleton<ISubmissionTransfer, NotConfiguredSubmissionTransfer>();  // or the host's own transfer

// The host adds its own authorization to the group returned.
app.MapSubmissionEndpoints().RequireAuthorization(policy);
```

`AddSubmissions(options => options.CollectionName = "...")` changes the collection, `submissions` by default.
`MapSubmissionEndpoints("prefix")` changes the route prefix, `submission` by default.

### Multiple installations in one host

Use a named installation for each independent workflow. Each has its own Mongo collection, transfer implementation,
retention options, scoped submission service, and cleanup worker. Existing `AddSubmissions()` and
`MapSubmissionEndpoints()` remain available for a single default installation and may coexist with named ones.

```csharp
services.AddNamedSubmissions<RegisterSubmissionTransfer>("register", options =>
{
    options.DraftExpiry = TimeSpan.FromDays(30);
});
services.AddNamedSubmissions<VetcheckSubmissionTransfer>("vetcheck", options =>
{
    options.DraftExpiry = TimeSpan.FromDays(7);
    options.ErrorExpiry = TimeSpan.FromDays(90);
});

app.MapNamedSubmissionEndpoints("register", "register-submissions").RequireAuthorization();
app.MapNamedSubmissionEndpoints("vetcheck", "vetcheck-submissions").RequireAuthorization();
```

The transfer classes above are host implementations of `ISubmissionTransfer`; their constructor dependencies are
resolved through DI. Collection names are derived automatically as `{name}-submissions`: `register-submissions`
and `vetcheck-submissions` in this example. Named installations ignore `CollectionName` overrides. Use distinct
installation names and route prefixes. Endpoint names are
prefixed with the installation name, so both groups can be exposed together without OpenAPI name collisions.

A named installation uses the shared `IMongoDatabase` by default. To use a different database, register an
`IMongoDatabase` keyed by the installation name before resolving the SDK services. Custom transfers can likewise be registered under that key before calling `AddNamedSubmissions`. Repositories
and MongoDB documents are internal implementation details.

Outside HTTP endpoints, resolve the installation's service from a scope using
`scope.ServiceProvider.GetRequiredKeyedService<ISubmissionService>("register")`. Its options are available through
`IOptionsMonitor<SubmissionOptions>.Get("register")`. Named routes resolve only their corresponding service, so
start/resume, list, animal updates, transfer, and retention cleanup stay within that installation.

### Endpoints

All take the CPH as `{county}/{parish}/{holding}` route segments.

| Method | Route | Does |
|---|---|---|
| GET | `/submission/{cph}` | Lists the CPH's submissions, newest first |
| GET | `/submission/{cph}/start` | Resumes the latest draft, creating one if none exists; `?key={submissionId}` resumes that draft |
| GET | `/submission/{cph}/{submissionId}` | Returns a submission and its animals |
| GET | `/submission/{cph}/{submissionId}/{animalId}` | Returns an animal's status and data |
| POST, PATCH | `/submission/{cph}/{submissionId}/{animalId}` | Replaces an animal's status and data, adding it if new |
| DELETE | `/submission/{cph}/{submissionId}/{animalId}` | Removes an animal from a draft |
| POST | `/submission/{cph}/{submissionId}` | Transfers the draft and saves submitted status and timestamp |

### Example URLs

With the named installations above hosted at `https://localhost:5001`, CPH `12/345/6789`, submission id
`ABC-DEF-GHJ`, and animal id `UK123123123`:

| Action | Method | Example URL |
|---|---|---|
| Start or resume the latest register draft | GET | `https://localhost:5001/register-submissions/12/345/6789/start` |
| Resume a specific register draft | GET | `https://localhost:5001/register-submissions/12/345/6789/start?key=ABC-DEF-GHJ` |
| List register submissions | GET | `https://localhost:5001/register-submissions/12/345/6789` |
| Get a register submission | GET | `https://localhost:5001/register-submissions/12/345/6789/ABC-DEF-GHJ` |
| Get an animal | GET | `https://localhost:5001/register-submissions/12/345/6789/ABC-DEF-GHJ/UK123123123` |
| Add or replace an animal's data | POST or PATCH | `https://localhost:5001/register-submissions/12/345/6789/ABC-DEF-GHJ/UK123123123` |
| Delete an animal from a draft | DELETE | `https://localhost:5001/register-submissions/12/345/6789/ABC-DEF-GHJ/UK123123123` |
| Submit the register draft | POST | `https://localhost:5001/register-submissions/12/345/6789/ABC-DEF-GHJ` |
| Start or resume the latest vetcheck draft | GET | `https://localhost:5001/vetcheck-submissions/12/345/6789/start` |
| Resume a specific vetcheck draft | GET | `https://localhost:5001/vetcheck-submissions/12/345/6789/start?key=PRS-PJY-WV8` |
| List vetcheck submissions | GET | `https://localhost:5001/vetcheck-submissions/12/345/6789` |

Use the submission id returned by that installation's start response in subsequent requests. A register submission
id cannot be used to resume or access a vetcheck submission. Omitting `key` resumes the latest draft for that CPH,
creating one only when none exists. Successful transfer confirms receipt and retains the local submission with `SUBMITTED` status and `SubmittedDate`.

For the default installation mapped with `app.MapSubmissionEndpoints()`, the equivalent start URL is
`https://localhost:5001/submission/12/345/6789/start`. Replace the example host and port with your service's address.

### Exceptions the host maps

The service throws these, so the host's exception handler must map them to problem details:

| Exception | Status |
|---|---|
| `Defra.Livestock.Sdk.Core.Exceptions.NotFoundException` | 404 |
| `Defra.Livestock.Sdk.Submissions.Exceptions.SubmissionConflictException` | 409 |
| `Defra.Livestock.Sdk.Submissions.Exceptions.BusinessRuleException`, `ArgumentException`, `BadHttpRequestException` | 400 |
| `Defra.Livestock.Sdk.Core.Exceptions.ServiceUnavailableException` | 503 |

### Transferring a submission

Implement `ISubmissionTransfer` to send a submitted draft downstream. Throw `ServiceUnavailableException` when the
downstream service cannot take it: the submission stays a draft and the caller can try again.
`NotConfiguredSubmissionTransfer` always does this, and stands in until a host's transfer is built.

Transfer implementations receive a `SubmissionTransferRequest` POCO containing the public `SubmissionId`, `Cph`,
`CreatedDate`, and all `Animals` with their status and full `DataItem` values. MongoDB document types, internal ids,
and optimistic concurrency versions are not exposed. Mapping creates a separate object graph so a transfer cannot
mutate stored submissions through its request.

When upgrading from the document-based transfer API, change `TransferAsync(SubmissionDocument, ...)` to
`TransferAsync(SubmissionTransferRequest, ...)` and use `SubmissionId` instead of `HashId`. Custom repository
implementations are no longer a public extension point; configure MongoDB through the database and installation options.


### Cleanup

`AddSubmissions()` registers a background service that cleans up at startup and hourly thereafter:

| Status | Retention option | Default | Age measured from |
|---|---|---|---|
| Draft | `DraftExpiry` | 30 days | `CreatedDate` |
| Submitted | `SubmittedExpiry` | 30 days | `SubmittedDate` |
| Error | `ErrorExpiry` | 30 days | `CreatedDate` |

All dates use UTC. Configured windows must be positive. Failed cleanup passes are logged and retried hourly.
Submitted records without a submission timestamp are retained rather than guessing their age.

```csharp
services.AddSubmissions(options =>
{
    options.DraftExpiry = TimeSpan.FromDays(30);
    options.SubmittedExpiry = TimeSpan.FromDays(30);
    options.ErrorExpiry = TimeSpan.FromDays(30);
});
```

The same options can be configured independently for each `AddNamedSubmissions` installation.
Successful completion of `ISubmissionTransfer.TransferAsync` confirms downstream receipt. The service saves
`SUBMITTED` status and the current UTC `SubmittedDate`, retaining the record for inspection until cleanup runs
at or after its submitted retention window. Submitted records remain readable but cannot be edited or resumed.
A failed or pending transfer does not mark a draft as submitted. Error submissions follow their configured creation-age retention window.

### Stored documents

Submission ids are `Guid.CreateDisplayHash()` from `Defra.Livestock.Sdk.Core` (`XXX-XXX-XXX`). Callers may send them
in lower case or with `O`, `I` or `L` for `0` and `1`; `SubmissionHashId.Normalise` reads those as the stored id.

The package registers camel-case element names for its own document types, so documents look the same whatever
conventions the host registers. The repository creates these indexes on first use:

| Index | Purpose |
|---|---|
| `unique_hashId` | Ids are unique; a clash is retried with a new GUID |
| `cph_createdDate` | Lists a CPH's submissions, newest first |

A CPH may have multiple drafts. A start without a key resumes its latest draft by creation date, creating one
only when no draft exists. Supplying
`?key={submissionId}` resumes that draft and preserves its animals and data. An unknown key or a draft from another
CPH returns 404; a submitted key returns 409. On first use, the repository removes the
legacy `unique_draft_per_cph` index if it exists. The database user needs permission to drop that index.

Saves check the document's `version`, so a change made by another request since the draft was read returns 409.

## Prerequisites

- .NET 10 SDK with C# 14 support. SDK selection and the Microsoft Testing Platform runner are configured in
  [global.json](global.json).
- To try a change in a consuming service: a local clone of [lis-dev](https://github.com/DEFRA/lis-dev), which runs a
  local NuGet server

## Setup

```bash
dotnet restore Submissions.slnx
dotnet build Submissions.slnx
```

## Running in development

This is a library, so it has no service of its own. To try a change in a consuming service before publishing, pack it
and push it to the local NuGet server that lis-dev runs on port 5555:

```bash
dotnet pack src/Submissions/Submissions.csproj -c Release -o ./artifacts /p:Version=0.0.0-local.1
dotnet nuget push "./artifacts/*.nupkg" --source http://localhost:5555/v3/index.json --api-key LOCAL-NUGET-API-KEY
```

Then reference that version from the consuming service.

## Running tests

Tests use xUnit v3 with Shouldly assertions and Microsoft Testing Platform. The endpoint tests run the endpoints in a
minimal host with an in-memory repository that round-trips documents through BSON, so they cover serialisation but not
MongoDB itself.

```bash
dotnet test --solution Submissions.slnx
```

## Pipelines

The workflows are the same as [lis-sdk-nuget-core](https://github.com/DEFRA/lis-sdk-nuget-core#pipelines): prereleases
from `feature/**` and `hotfix/**` branches, validation on pull requests, a stable release on merge to `main`, and a
tag-triggered NuGet publish. NuGet's trusted publishing policy must use repository owner `DEFRA`, repository
`lis-sdk-nuget-submissions`, workflow filename `publish.yml` and environment `release`.

## Branching policy

The repo uses mainline development. Branch from `main` and name the branch `feature/LREG-<ticket>-<description>` or
`hotfix/LREG-<ticket>-<description>`. Rebase onto `main` rather than merging `main` in.

## Maintaining public APIs

The library uses `Microsoft.CodeAnalysis.PublicApiAnalyzers`. Update `src/Submissions/PublicAPI.Shipped.txt` in the
same PR as an API change, and keep `PublicAPI.Unshipped.txt` empty apart from `#nullable enable`.

## Licence

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

The following attribution statement MUST be cited in your products and applications when using this information.

> Contains public sector information licensed under the Open Government licence v3
