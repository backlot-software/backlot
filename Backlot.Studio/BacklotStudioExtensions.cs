using Backlot.Studio.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Backlot.Studio;

/// <summary>
/// Mounts the Backlot Studio admin UI inside any ASP.NET Core host:
/// <code>
/// builder.Services.AddBacklotStudio(o => o.BaseUrl = "https://localhost:7221");
/// ...
/// app.MapBacklotStudio("/studio");
/// </code>
/// </summary>
public static class BacklotStudioExtensions
{
    private static readonly Lazy<IFileProvider> EmbeddedAssets = new(() =>
        new ManifestEmbeddedFileProvider(typeof(BacklotStudioExtensions).Assembly, "wwwroot"));

    /// <summary>
    /// Registers everything the Studio pages need: its own cookie authentication scheme and
    /// authorization policy, session state, the Backlot API client, and the Razor Pages area.
    /// </summary>
    public static IServiceCollection AddBacklotStudio(
        this IServiceCollection services,
        Action<BacklotStudioOptions>? configure = null)
        => services.AddBacklotStudio(configuration: null, configure);

    /// <summary>
    /// Same as <see cref="AddBacklotStudio(IServiceCollection, Action{BacklotStudioOptions}?)"/>, but
    /// binds the <c>BacklotStudio</c> configuration section first; <paramref name="configure"/> then
    /// overrides whatever configuration supplied.
    /// </summary>
    public static IServiceCollection AddBacklotStudio(
        this IServiceCollection services,
        IConfiguration? configuration,
        Action<BacklotStudioOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(d => d.ServiceType == typeof(BacklotStudioOptions)))
        {
            throw new InvalidOperationException(
                $"{nameof(AddBacklotStudio)} has already been called on this service collection.");
        }

        var options = new BacklotStudioOptions();
        configuration?.GetSection(BacklotStudioOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        // An empty BaseUrl means "the API is this same host", which is the normal case now that the
        // Studio is mounted by BuildWebApp. The address is then resolved per request, below.
        Uri? baseUri = null;
        if (!string.IsNullOrWhiteSpace(options.BaseUrl) &&
            !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out baseUri))
        {
            throw new InvalidOperationException(
                $"{nameof(BacklotStudioOptions)}.{nameof(BacklotStudioOptions.BaseUrl)} must be an absolute URI " +
                $"or empty (to call the host the Studio is mounted in), but was '{options.BaseUrl}'.");
        }

        // Registered as a plain singleton (not just IOptions) because MapBacklotStudio mutates the
        // same instance to apply a mount path given at map time.
        services.AddSingleton(options);
        services.TryAddSingleton<IOptions<BacklotStudioOptions>>(new OptionsWrapper<BacklotStudioOptions>(options));

        // TryAdd inside, so a host that already registered Redis or SQL Server distributed cache keeps it.
        services.AddDistributedMemoryCache();
        services.AddSession();
        services.AddHttpContextAccessor();
        services.AddTransient<BasicAuthHandler>();

        // Resolved per resolution rather than once at registration: IHttpClientFactory hands out a
        // fresh HttpClient for every typed-client resolution (only the handler is pooled) and the
        // typed client is scoped, so a self-address computed here is the current request's.
        services.AddHttpClient<IBacklotApiClient, BacklotApiClient>((provider, client) =>
                client.BaseAddress = baseUri ?? ResolveSelfBaseAddress(provider))
            .AddHttpMessageHandler<BasicAuthHandler>();

        AddStudioAuthentication(services, options);
        AddStudioRazorPages(services, options);

        return services;
    }

    /// <summary>
    /// Serves the Studio: its embedded static assets, its session, and its Razor Pages.
    /// </summary>
    /// <param name="app">The host application.</param>
    /// <param name="prefix">
    /// Path to mount on, e.g. <c>/studio</c>. When omitted the value configured on
    /// <see cref="BacklotStudioOptions.PathPrefix"/> is used (<c>/studio</c> by default).
    /// </param>
    public static WebApplication MapBacklotStudio(this WebApplication app, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.Services.GetService<BacklotStudioOptions>()
            ?? throw new InvalidOperationException(
                $"{nameof(AddBacklotStudio)}() must be called on the service collection before " +
                $"{nameof(MapBacklotStudio)}().");

        if (prefix is not null)
        {
            options.PathPrefix = prefix;
        }

        // The embedded wwwroot. Nothing routes to this path, so the static file middleware picks it
        // up even though it sits behind the host's routing middleware.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = EmbeddedAssets.Value,
            RequestPath = options.AssetRequestPath,
        });

        // Session is branched onto the Studio's own path so a host that runs without sessions — or
        // with differently configured ones — is left untouched.
        var sessionOptions = BuildSessionOptions(options);
        var studioPath = options.PathBase;
        app.UseWhen(
            context => !studioPath.HasValue || context.Request.Path.StartsWithSegments(studioPath),
            branch => branch.UseSession(sessionOptions));

        // Safe to call even when the host already maps Razor Pages: MapRazorPages reuses the
        // existing page data source rather than adding a second one.
        app.MapRazorPages();

        return app;
    }

    private static void AddStudioAuthentication(IServiceCollection services, BacklotStudioOptions options)
    {
        // AddAuthentication() without a scheme name leaves the host's default schemes alone.
        services.AddAuthentication()
            .AddCookie(BacklotStudioDefaults.AuthenticationScheme, cookie =>
            {
                // Read lazily, on first use, so a path passed to MapBacklotStudio is reflected here.
                cookie.LoginPath = options.PathPrefix + "/login";
                cookie.LogoutPath = options.PathPrefix + "/logout";
                cookie.AccessDeniedPath = options.PathPrefix + "/login";
                cookie.ExpireTimeSpan = options.IdleTimeout;
                cookie.SlidingExpiration = true;
                cookie.Cookie.Name = BacklotStudioDefaults.AuthenticationCookieName;
                cookie.Cookie.Path = options.CookiePath;
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SameSite = SameSiteMode.Strict;
                cookie.Cookie.SecurePolicy = options.CookieSecurePolicy;
                options.ConfigureCookie?.Invoke(cookie);
            });

        // A named policy rather than a fallback policy: the host's own endpoints must stay as they were.
        services.AddAuthorization(authorization =>
            authorization.AddPolicy(BacklotStudioDefaults.AuthorizationPolicy, policy => policy
                .AddAuthenticationSchemes(BacklotStudioDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()));
    }

    private static void AddStudioRazorPages(IServiceCollection services, BacklotStudioOptions options)
    {
        services.AddRazorPages()
            .ConfigureApplicationPartManager(manager =>
            {
                // Only needed when the Studio assembly is not part of the host's dependency context
                // (for instance when it is loaded dynamically). Adding it twice would duplicate every page.
                var assembly = typeof(BacklotStudioExtensions).Assembly;
                var name = assembly.GetName().Name!;
                if (manager.ApplicationParts.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                foreach (var part in ApplicationPartFactory.GetApplicationPartFactory(assembly).GetApplicationParts(assembly))
                {
                    manager.ApplicationParts.Add(part);
                }
            });

        services.Configure<RazorPagesOptions>(razorPages =>
        {
            // Scoped to the area, so the host's own pages keep their own (or no) authorization.
            razorPages.Conventions.AuthorizeAreaFolder(
                BacklotStudioDefaults.AreaName, "/", BacklotStudioDefaults.AuthorizationPolicy);
            razorPages.Conventions.AllowAnonymousToAreaPage(BacklotStudioDefaults.AreaName, "/Login");

            // Every Studio page declares an absolute route (@page "/roles/{RoleType?}"), which is
            // rewritten here to sit under the mount path. Conventions run when the page routes are
            // first built — after MapBacklotStudio — so the prefix read here is the final one.
            razorPages.Conventions.AddAreaFolderRouteModelConvention(
                BacklotStudioDefaults.AreaName, "/", model =>
                {
                    var prefix = options.RouteTemplatePrefix;
                    if (prefix.Length == 0) return;

                    foreach (var selector in model.Selectors)
                    {
                        if (selector.AttributeRouteModel is not { } route) continue;

                        var template = route.Template?.TrimStart('/') ?? string.Empty;
                        route.Template = template.Length == 0 ? prefix : $"{prefix}/{template}";
                    }
                });
        });
    }

    /// <summary>
    /// Works out the address of the host the Studio is mounted in, for the case where no explicit
    /// <see cref="BacklotStudioOptions.BaseUrl"/> was configured.
    /// </summary>
    /// <remarks>
    /// The server's own listening addresses come first, preferring a plain http listener: a loopback
    /// call to an https listener has to satisfy full certificate validation, and the ASP.NET dev
    /// certificate is frequently not in the trust store the runtime reads on Linux -- which shows up
    /// as a Studio that cannot sign in while the browser works fine. Going straight to the local
    /// listener also avoids routing a self-call back out through a container's ingress.
    ///
    /// Note that a self-call presents its own address as the Host header, so a host that narrows
    /// AllowedHosts away from "*" has to include it.
    /// </remarks>
    private static Uri ResolveSelfBaseAddress(IServiceProvider provider)
    {
        var pathBase = provider.GetService<IHttpContextAccessor>()?.HttpContext?.Request.PathBase.Value ?? string.Empty;

        var addresses = provider.GetService<IServer>()?.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is { Count: > 0 })
        {
            var address = addresses.FirstOrDefault(a => a.StartsWith(Uri.UriSchemeHttp + "://", StringComparison.OrdinalIgnoreCase))
                          ?? addresses.First();

            // Kestrel reports wildcard binds as http://+:8080 or http://[::]:8080; neither is dialable.
            var dialable = address
                .Replace("://+", "://127.0.0.1")
                .Replace("://*", "://127.0.0.1")
                .Replace("://[::]", "://127.0.0.1");

            if (Uri.TryCreate(Combine(dialable, pathBase), UriKind.Absolute, out var fromServer))
                return fromServer;
        }

        // No server addresses (an in-memory test server, for instance): fall back to the request.
        var request = provider.GetService<IHttpContextAccessor>()?.HttpContext?.Request;
        if (request is not null &&
            Uri.TryCreate(Combine($"{request.Scheme}://{request.Host}", pathBase), UriKind.Absolute, out var fromRequest))
            return fromRequest;

        throw new InvalidOperationException(
            "Backlot Studio could not determine the address of the Backlot API it is mounted in. " +
            $"Set {BacklotStudioOptions.SectionName}:{nameof(BacklotStudioOptions.BaseUrl)} to the API's absolute URL.");
    }

    private static string Combine(string origin, string pathBase) =>
        $"{origin.TrimEnd('/')}/{pathBase.Trim('/')}".TrimEnd('/') + "/";

    private static SessionOptions BuildSessionOptions(BacklotStudioOptions options)
    {
        var sessionOptions = new SessionOptions
        {
            IdleTimeout = options.IdleTimeout,
        };

        sessionOptions.Cookie.Name = BacklotStudioDefaults.SessionCookieName;
        sessionOptions.Cookie.Path = options.CookiePath;
        sessionOptions.Cookie.HttpOnly = true;
        sessionOptions.Cookie.IsEssential = true;
        sessionOptions.Cookie.SameSite = SameSiteMode.Strict;
        sessionOptions.Cookie.SecurePolicy = options.CookieSecurePolicy;
        options.ConfigureSession?.Invoke(sessionOptions);

        return sessionOptions;
    }
}
