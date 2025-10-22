using DotNetEnv;
using McpServer;

var builder = WebApplication.CreateBuilder(args);

Env.Load("../.env");

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);
});
builder.Services.AddHttpClient<IOdooClient, OdooClient>();      
var app = builder.Build();

app.MapMcp();

app.Run();
