using Frontend.Abstractions;
using Frontend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection("Gateway"));

builder.Services.AddHttpClient<IGozonApiClient, GozonApiClient>();

var app = builder.Build();

app.UseStaticFiles();
app.MapRazorPages();

app.Run();
