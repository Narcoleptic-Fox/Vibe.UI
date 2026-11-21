using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Vibe.UI;
using Vibe.UI.Docs;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add Vibe.UI services (Toast and Dialog only - theming is pure CSS now)
builder.Services.AddVibeUI(options =>
{
    // Zinc matches the zinc utility classes the docs pages are written with, so the page
    // background, cards and code blocks share one neutral palette in both themes.
    options.BaseColor = "Zinc";
});

await builder.Build().RunAsync();
