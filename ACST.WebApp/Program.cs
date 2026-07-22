using ACST.WebApp;
using ACST.WebApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Connect to the ASP.NET Core Backend API
// Dynamically select HTTP/HTTPS backend port based on the active client scheme to handle certificate trust issues.
var apiBaseUrl = new Uri(builder.HostEnvironment.BaseAddress).Scheme == "https"
    ? "https://localhost:7019"
    : "http://localhost:5211";

builder.Services.AddScoped(sp =>
{
    var js = sp.GetRequiredService<IJSRuntime>();
    var handler = new CustomAuthorizationHandler(js)
    {
        InnerHandler = new HttpClientHandler()
    };
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

builder.Services.AddScoped<AuthStateService>();

await builder.Build().RunAsync();
