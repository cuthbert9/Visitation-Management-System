using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VisitorManagementSystem.Shared.Models;
using VMS.Web;
using VMS.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001/";

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

builder.Services.AddScoped<IVisitorService, VisitorService>();
builder.Services.AddScoped<IVisitService, VisitService>();
builder.Services.AddScoped<IOfficeService, OfficeService>();

await builder.Build().RunAsync();
