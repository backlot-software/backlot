using System;
using Backlot.Defaults.Scenarios.Configuration.Models;

namespace Backlot.Experimental.WebApp.Services;

/// <summary>
/// Typed request and response schema definition for a specific endpoint.
/// Defaults are derived automatically from <see cref="Scenarios.Play()"/> via
/// <see cref="BacklotOpenApiDocument.BuildDefinitions"/>.
/// Add entries to <see cref="BacklotOpenApiDocument.EndpointDefinitions"/> to
/// customize the schema for specific endpoints.
/// </summary>
public sealed record EndpointDefinition
{
    /// <summary>
    /// Role items and characteristics for the request body.
    /// Single entry: properties are written flat.
    /// Multiple entries: each type is nested under its role name.
    /// </summary>
    public RoleResultItem[] RequestTypes { get; init; } = [];

    /// <summary>
    /// C# type for the response "Body" property.
    /// </summary>
    public Type ResponseType { get; init; }
}