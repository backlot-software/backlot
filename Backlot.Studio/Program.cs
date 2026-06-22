using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Backlot.Studio.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);        // D-04: 8-hour workday timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);  // D-04/D-05: must match session IdleTimeout
        options.SlidingExpiration = true;                 // D-05: reset expiry on each request
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BasicAuthHandler>();

builder.Services.AddHttpClient<IBacklotApiClient, BacklotApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BacklotApi:BaseUrl"]
        ?? "https://localhost:7221");
}).AddHttpMessageHandler<BasicAuthHandler>();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();             // must come before UseAuthentication so session is available during auth
app.UseAuthentication();      // must come before UseAuthorization
app.UseAuthorization();
app.MapRazorPages();

app.Run();
