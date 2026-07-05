# SharedCommon.ApiVersioning

API versioning infrastructure wrapping Asp.Versioning.Mvc 8.

## API Surface

- `AddSharedApiVersioning(IConfiguration)` — registers versioning and API Explorer groups
- `ApiVersioningOptions` — `DefaultVersion`, `AssumeDefaultWhenUnspecified`, `ReportApiVersions`, `VersionReadingStrategy`
- `VersionReadingStrategy` — individually toggle URL-segment, query-string, header, media-type readers

## Rules

**Must:**
- URL segment versioning is on by default (`/api/v{version}/...`)
- All versioned controllers use `[ApiVersion("x.y")]` attribute
- Route template uses `v{version:apiVersion}` constraint
- Deprecated versions use `[ApiVersion("1.0", Deprecated = true)]`
- API Explorer groups named `"v{version}"` for Swagger generation

**Forbidden:**
- Hardcoding version numbers in route strings without the route constraint
- More than one major version active at a time without deprecation of the prior
- Mixing versioning strategies without a clear migration guide

## Design Decisions

URL-segment versioning is the default because it is cacheable, human-readable,
and does not pollute request headers or query strings.

## Test Strategy

- Unit test `ApiVersioningOptions` defaults
- Integration tests verify `/api/v1/...` and `/api/v2/...` route to correct controllers
- Test deprecated version returns `api-deprecated-versions` response header

## Extension Points

- Add custom `IApiVersionReader` to `VersionReadingStrategy`
- Register Swagger gen per group via `IApiVersionDescriptionProvider`
