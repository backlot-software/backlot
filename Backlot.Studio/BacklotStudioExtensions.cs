using Backlot.Authentication.Razor;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Backlot.Studio;

/// <summary>
/// Mounts the Backlot Studio admin UI inside any ASP.NET Core host:
/// <code>
/// builder.Services.AddBacklotStudio(configureSignIn: o => o.BaseUrl = "https://localhost:7221");
/// ...
/// app.MapBacklotStudio("/studio");
/// </code>
/// </summary>
public static class BacklotStudioExtensions
{
    private static readonly Lazy<IFileProvider> EmbeddedAssets = new(() =>
        new ManifestEmbeddedFileProvider(typeof(BacklotStudioExtensions).Assembly, "wwwroot"));

    /// <summary>
    /// Registers everything the Studio pages need: the Backlot sign-in (see
    /// <see cref="BacklotSignInExtensions.AddBacklotSignIn(IServiceCollection, IConfiguration?, Action{BacklotSignInOptions}?)"/>)
    /// and the Razor Pages area.
    /// </summary>
    public static IServiceCollection AddBacklotStudio(
        this IServiceCollection services,
        Action<BacklotStudioOptions>? configure = null,
        Action<BacklotSignInOptions>? configureSignIn = null)
        => services.AddBacklotStudio(configuration: null, configure, configureSignIn);

    /// <summary>
    /// Same as <see cref="AddBacklotStudio(IServiceCollection, Action{BacklotStudioOptions}?, Action{BacklotSignInOptions}?)"/>, but
    /// binds the <c>BacklotStudio</c> configuration section first; <paramref name="configure"/> then
    /// overrides whatever configuration supplied. The <c>BacklotSignIn</c> section is bound for the
    /// sign-in, <paramref name="configureSignIn"/> then overrides it.
    /// </summary>
    public static IServiceCollection AddBacklotStudio(
        this IServiceCollection services,
        IConfiguration? configuration,
        Action<BacklotStudioOptions>? configure = null,
        Action<BacklotSignInOptions>? configureSignIn = null)
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

        // Registered as a plain singleton (not just IOptions) because MapBacklotStudio mutates the
        // same instance to apply a mount path given at map time.
        services.AddSingleton(options);
        services.TryAddSingleton<IOptions<BacklotStudioOptions>>(new OptionsWrapper<BacklotStudioOptions>(options));

        services.AddBacklotSignIn(configuration, signIn =>
        {
            // BaseUrl used to live in the BacklotStudio section; still honoured when BacklotSignIn has none.
            if (string.IsNullOrWhiteSpace(signIn.BaseUrl))
                signIn.BaseUrl = configuration?[$"{BacklotStudioOptions.SectionName}:{nameof(BacklotSignInOptions.BaseUrl)}"] ?? string.Empty;

            // Unless the host picks its own, the login pages sit under the Studio's mount path
            // (/studio/login, /studio/login/bearer) and a sign-in lands on the Studio dashboard.
            // Read lazily, so a path passed to MapBacklotStudio is reflected.
            signIn.PathPrefixFallback = () => options.PathPrefix;
            signIn.DefaultReturnUrlFallback = () => options.PathPrefix.Length == 0 ? "/" : options.PathPrefix;

            configureSignIn?.Invoke(signIn);
        });

        AddStudioRazorPages(services, options);

        return services;
    }

    /// <summary>
    /// Serves the Studio: its embedded static assets, the Backlot sign-in, and its Razor Pages.
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

        // The sign-in's session (for the whole host) and the Razor Pages, the Studio's included.
        app.MapBacklotSignIn();

        return app;
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
            razorPages.Conventions.RequireBacklotSignInForArea(BacklotStudioDefaults.AreaName);

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
}
