using ACST.WebApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Connect to the ASP.NET Core Backend API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7019") });

await builder.Build().RunAsync();
