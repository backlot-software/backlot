using Backlot.Authentication.Razor.Core;
using Backlot.Authentication.Razor.Implementation;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Backlot.Authentication.Razor;

/// <summary>
/// Signs users in to a Backlot API once for the whole host: Backlot Studio and the host's own
/// Razor Pages share the sign-in.
/// <code>
/// builder.Services.AddBacklotSignIn(builder.Configuration);
/// ...
/// app.MapBacklotSignIn();
/// </code>
/// Backlot Studio calls both itself; a host using the Studio only calls <c>AddBacklotSignIn</c> to
/// configure the sign-in.
/// </summary>
public static class BacklotSignInExtensions
{
    // ((IApplicationBuilder)app).Properties key marking that MapBacklotSignIn ran, so the Studio and the host can both call it.
    private const string MappedMarker = "Backlot.Authentication.Razor.Mapped";

    /// <summary>
    /// Registers the sign-in: its cookie authentication scheme and authorization policy, session
    /// state, the per-user <see cref="IBacklotApiClient"/>, <see cref="IBacklotSignInManager"/> and
    /// the login pages.
    /// </summary>
    /// <remarks>
    /// Safe to call more than once: later calls only apply <paramref name="configure"/> to the
    /// options registered by the first one.
    /// </remarks>
    public static IServiceCollection AddBacklotSignIn(
        this IServiceCollection services,
        Action<BacklotSignInOptions>? configure = null)
        => services.AddBacklotSignIn(configuration: null, configure);

    /// <summary>
    /// Same as <see cref="AddBacklotSignIn(IServiceCollection, Action{BacklotSignInOptions}?)"/>, but
    /// binds the <c>BacklotSignIn</c> configuration section first; <paramref name="configure"/> then
    /// overrides whatever configuration supplied.
    /// </summary>
    public static IServiceCollection AddBacklotSignIn(
        this IServiceCollection services,
        IConfiguration? configuration,
        Action<BacklotSignInOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.FirstOrDefault(d => d.ServiceType == typeof(BacklotSignInOptions))
                ?.ImplementationInstance is BacklotSignInOptions existing)
        {
            configuration?.GetSection(BacklotSignInOptions.SectionName).Bind(existing);
            configure?.Invoke(existing);
            return services;
        }

        var options = new BacklotSignInOptions();
        configuration?.GetSection(BacklotSignInOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        // Registered as a plain singleton (not just IOptions) because later AddBacklotSignIn calls
        // and MapBacklotSignIn mutate the same instance.
        services.AddSingleton(options);
        services.TryAddSingleton<IOptions<BacklotSignInOptions>>(new OptionsWrapper<BacklotSignInOptions>(options));

        // TryAdd inside, so a host that already registered Redis or SQL Server distributed cache keeps it.
        services.AddDistributedMemoryCache();
        services.AddSession();
        services.AddHttpContextAccessor();
        services.AddTransient<AuthHeaderHandler>();
        services.TryAddScoped<IBacklotSignInManager, BacklotSignInManager>();

        // BaseUrl is required and never inferred, and read when the client is created so a later
        // AddBacklotSignIn call can still set it. When it is missing BaseAddress stays null:
        // BacklotApiClient then reports itself as unconfigured (and throws on use) instead of the
        // whole page failing to be constructed, which is what lets the login page render its alert.
        services.AddHttpClient<IBacklotApiClient, BacklotApiClient>((provider, client) =>
                client.BaseAddress = provider.GetRequiredService<BacklotSignInOptions>().BaseUri)
            .AddHttpMessageHandler<AuthHeaderHandler>();

        AddAuthentication(services, options);
        AddRazorPages(services, options);

        return services;
    }

    /// <summary>
    /// Serves the sign-in: the session for every request of the host, and the login pages.
    /// </summary>
    /// <param name="app">The host application.</param>
    /// <param name="prefix">
    /// Path the login pages are mounted on, e.g. <c>/account</c>. When omitted the value configured
    /// on <see cref="BacklotSignInOptions.PathPrefix"/> is used.
    /// </param>
    /// <remarks>
    /// This is the host's session: a host using the Backlot sign-in must not add a <c>UseSession</c>
    /// of its own, it shares this one instead.
    /// </remarks>
    public static WebApplication MapBacklotSignIn(this WebApplication app, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.Services.GetService<BacklotSignInOptions>()
            ?? throw new InvalidOperationException(
                $"{nameof(AddBacklotSignIn)}() must be called on the service collection before " +
                $"{nameof(MapBacklotSignIn)}().");

        if (prefix is not null)
        {
            options.PathPrefix = prefix;
        }

        if (((IApplicationBuilder)app).Properties.ContainsKey(MappedMarker))
        {
            return app;
        }

        ((IApplicationBuilder)app).Properties[MappedMarker] = true;

        // For every request: the sign-in is shared by the Studio and the host's own pages. The
        // session is only loaded, and its cookie only issued, when a request actually uses it.
        app.UseSession(BuildSessionOptions(options));

        // Safe to call even when the host already maps Razor Pages: MapRazorPages reuses the
        // existing page data source rather than adding a second one.
        app.MapRazorPages();

        return app;
    }

    private static void AddAuthentication(IServiceCollection services, BacklotSignInOptions options)
    {
        // AddAuthentication() without a scheme name leaves the host's default schemes alone.
        services.AddAuthentication()
            .AddCookie(BacklotSignInDefaults.AuthenticationScheme, cookie =>
            {
                // Read lazily, on first use, so a path passed to MapBacklotSignIn is reflected here.
                cookie.LoginPath = options.EffectiveLoginPath;
                cookie.LogoutPath = options.EffectivePathPrefix + "/logout";
                cookie.AccessDeniedPath = options.EffectiveLoginPath;
                cookie.ExpireTimeSpan = options.IdleTimeout;
                cookie.SlidingExpiration = true;
                cookie.Cookie.Name = BacklotSignInDefaults.AuthenticationCookieName;
                // The whole host, so one sign-in covers the Studio and the host's own pages.
                cookie.Cookie.Path = "/";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SameSite = SameSiteMode.Strict;
                cookie.Cookie.SecurePolicy = options.CookieSecurePolicy;
                options.ConfigureCookie?.Invoke(cookie);
            });

        // A named policy rather than a fallback policy: the host's own endpoints must stay as they
        // were until the host opts them in.
        services.AddAuthorization(authorization =>
            authorization.AddPolicy(BacklotSignInDefaults.AuthorizationPolicy, policy => policy
                .AddAuthenticationSchemes(BacklotSignInDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()));
    }

    private static void AddRazorPages(IServiceCollection services, BacklotSignInOptions options)
    {
        services.AddRazorPages()
            .ConfigureApplicationPartManager(manager =>
            {
                // Only needed when this assembly is not part of the host's dependency context
                // (for instance when it is loaded dynamically). Adding it twice would duplicate every page.
                var assembly = typeof(BacklotSignInExtensions).Assembly;
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
            razorPages.Conventions.AllowAnonymousToAreaFolder(BacklotSignInDefaults.AreaName, "/");

            // Every login page declares an absolute route (@page "/login"), which is rewritten here to
            // sit under the mount path. Conventions run when the page routes are first built — after
            // MapBacklotSignIn — so the prefix read here is the final one.
            razorPages.Conventions.AddAreaFolderRouteModelConvention(
                BacklotSignInDefaults.AreaName, "/", model =>
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

    private static SessionOptions BuildSessionOptions(BacklotSignInOptions options)
    {
        var sessionOptions = new SessionOptions
        {
            IdleTimeout = options.IdleTimeout,
        };

        sessionOptions.Cookie.Name = BacklotSignInDefaults.SessionCookieName;
        sessionOptions.Cookie.Path = "/";
        sessionOptions.Cookie.HttpOnly = true;
        sessionOptions.Cookie.IsEssential = true;
        sessionOptions.Cookie.SameSite = SameSiteMode.Strict;
        sessionOptions.Cookie.SecurePolicy = options.CookieSecurePolicy;
        options.ConfigureSession?.Invoke(sessionOptions);

        return sessionOptions;
    }
}
