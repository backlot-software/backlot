using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Auth, Session, HttpClient — registered in Plan 01-02/01-03
// builder.Services.AddDistributedMemoryCache();
// builder.Services.AddSession(...);
// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...);
// builder.Services.AddHttpContextAccessor();
// builder.Services.AddTransient<BasicAuthHandler>();
// builder.Services.AddHttpClient<IBacklotApiClient, BacklotApiClient>(...).AddHttpMessageHandler<BasicAuthHandler>();

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

// UseAuthentication/UseAuthorization/UseSession placeholders — wired in Plan 01-03
// app.UseAuthentication();
// app.UseAuthorization();
// app.UseSession();

app.MapRazorPages();

app.Run();
