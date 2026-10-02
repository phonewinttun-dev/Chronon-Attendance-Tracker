using ACST.WebApp;
using ACST.WebApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Connect to the ASP.NET Core Backend API
var isHttps = new Uri(builder.HostEnvironment.BaseAddress).Scheme == "https";
var configuredUrl = isHttps ? builder.Configuration["HttpsApiBaseUrl"] : builder.Configuration["HttpApiBaseUrl"];
var apiBaseUrl = configuredUrl ?? builder.Configuration["ApiBaseUrl"];

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    apiBaseUrl = isHttps ? "https://localhost:7019/" : "http://localhost:5211/";
}

if (!apiBaseUrl.EndsWith("/"))
{
    apiBaseUrl += "/";
}

builder.Services.AddTransient<CustomAuthorizationHandler>();

builder.Services.AddHttpClient("ChrononApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<CustomAuthorizationHandler>()
.AddStandardResilienceHandler();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChrononApi"));

builder.Services.AddScoped<AuthStateService>();

await builder.Build().RunAsync();
