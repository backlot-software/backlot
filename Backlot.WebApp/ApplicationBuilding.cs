using System;
using System.Diagnostics;
using System.Net;
using Backlot.Core;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Exceptions;
using Backlot.Core.Services;
using Backlot.Experimental.WebApp.Services;
using Backlot.Http;
using Backlot.Http.Media;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Scalar.AspNetCore;

namespace Backlot.WebApp;

public static class ApplicationBuilding
{
    private static ILogger Logger = ServiceLocator.GetLog<ILogger<WebApplicationBuilder>>();
    /// <summary>
    /// Build a Aspnet Core compatible webapplication.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureHostBuilder"></param>
    /// <param name="enableHttps">Enforce https redirection</param>
    /// <returns></returns>
    /// <exception cref="BadRequestException"></exception>
    /// <exception cref="NotFoundException"></exception>
    public static WebApplication BuildWebApp(this WebApplicationBuilder builder,
        Action<ConfigureHostBuilder> configureHostBuilder, bool enableHttps = true)
    {
        
        configureHostBuilder(builder.Host);

        builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BacklotOpenApiDocument>());
        // for documentation purposes; Backlot relies on NewtonsoftJson telling .netcore to use this for serialization needs to be done with: builder.Services.AddControllers().AddNewtonsoftJson();
        // However we use; Note: Minimal APIs, and have return ContentResult with manually serialized JSON.

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            context.Request.EnableBuffering(); // TODO: this can likely be optimized.
            ServiceLocator.Configure(context.RequestServices);
            await next();
        });

        app.MapOpenApi();
        app.MapScalarApiReference();
        
        if(enableHttps)
            app.UseHttpsRedirection();

        app.MapGet("api/status", async (HttpContext ctx,
            [FromServices] IMediaFormatResolver mediaResolver) =>
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            return await mediaResolver.GetResultContent(ctx.Request,
                new
                {
                    TimeInMs = stopwatch.ElapsedMilliseconds,
                    Body = $"Backlot - version {typeof(IDirector).Assembly.GetName().Version}.",
                    Status = HttpStatusCode.OK,
                    ExcecutionTime = DateTimeOffset.Now
                }, stopwatch, Logger);
        });
        
        app.MapGet("api/role/{rolename}/{scenario}", async (
                string rolename,
                string scenario,
                HttpContext context,
                [FromServices] IPersistedRoleRepository roleRepository,
                [FromServices] IMediaFormatResolver mediaResolver) =>
            {
                var req = context.Request;
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                try
                {
                    if (req.ContentLength > 0)
                        throw new BadRequestException("GET requests should not contain a body.");

                    var roleType = Loader.GetRoleByName(rolename);

                    IRole role;

                    if (typeof(IDirector).IsAssignableFrom(roleType))
                    {
                        role = ServiceLocator.Get<IDirector>();
                    }
                    else
                    {
                        if (req.Query["uid"] == StringValues.Empty || req.Query.Count > 1)
                            throw new BadRequestException(
                                "The query string must contain only 'uid' when using roles other than director in GET requests.");

                        if (!roleRepository.TryGet(req.Query["uid"], roleType, out role))
                        {
                            throw new NotFoundException($"Role with uid '{req.Query["uid"]}' not found.");
                        }
                    }

                    var returnObj = await (new[] { role }).PlayAuthAsync(scenario);
                    return await mediaResolver.GetResultContent(req, returnObj, stopwatch, logger: Logger);
                }
                catch (Exception ex)
                {
                    return await mediaResolver.GetResultContent(req, ex, stopwatch, Logger);
                }
            })
            .WithName("PlayGet");

        app.MapPost("api/role/{rolename}/{scenario}", async (
                string rolename,
                string scenario,
                HttpContext context,
                [FromServices] IPersistedRoleRepository roleRepository,
                [FromServices] IMediaFormatResolver mediaResolver) =>
            {
                var req = context.Request;
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                try
                {
                    if (req.ContentLength == 0)
                        throw new BadRequestException("POST requests must contain a body.");

                    if (req.Query.Count > 0)
                        throw new BadRequestException("POST requests should not contain query strings.");

                    var roles = await GetRoles.ForPostRequest(req.Body, rolename, roleRepository);

                    var returnObj =  await roles.PlayAuthAsync(scenario);
                    return await mediaResolver.GetResultContent(req, returnObj, stopwatch, Logger);
                }
                catch (Exception ex)
                {
                    return await mediaResolver.GetResultContent(req, ex, stopwatch, Logger);
                }
            })
            .WithName("PlayPost");

        return app;
    }
}