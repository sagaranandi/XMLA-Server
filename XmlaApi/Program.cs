using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using XmlaApi.Services;
using System.Net.WebSockets;
using System.Threading.Tasks;
using XmlaApi.DaxQueryGeneration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSingleton<DaxQueryBuilder>();

builder.Services.AddSingleton<XmlaService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<XmlaService>>();
    var powerBiDomainURL = configuration["PowerBi:DomainURL"];
    return new XmlaService(logger, powerBiDomainURL);
});

var app = builder.Build();

// Enable WebSocket
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var xmlaService = context.RequestServices.GetRequiredService<XmlaService>();
            //await xmlaService.HandleWebSocketAsync(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

app.UseRouting();
app.MapControllers();
// Start the application
app.Run();
