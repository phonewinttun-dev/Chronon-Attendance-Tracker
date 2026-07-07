using ACST.WebApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Connect to the ASP.NET Core Backend API
// Dynamically select HTTP/HTTPS backend port based on the active client scheme to handle certificate trust issues.
var apiBaseUrl = new Uri(builder.HostEnvironment.BaseAddress).Scheme == "https"
    ? "https://localhost:7019"
    : "http://localhost:5211";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

await builder.Build().RunAsync();
