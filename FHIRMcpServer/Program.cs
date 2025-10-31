using DotNetEnv;
using FHIRMcpServer;
using FHIRMcpServer.DbContext;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

Env.Load("../.env");
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(builder.Configuration.GetValue<int>("FHIR_HOST_PORT"));
}); 
builder.Services.AddDbContext<ApplicationDbContext>( opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<IEpicClient, EpicClient>();
builder.Services.AddScoped<IPostgresService, PostgresService>();
var app = builder.Build();

app.MapMcp();

app.Run();
