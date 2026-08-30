using International.EInvoicing.Playground;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Everything runs in the browser: there is no HttpClient here, because nothing is ever sent anywhere.
builder.Services.AddSingleton<International.EInvoicing.Playground.Services.DocumentInspector>();

await builder.Build().RunAsync();
