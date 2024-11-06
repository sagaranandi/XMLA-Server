using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XmlaApi.Services;
using XmlaApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // Adds MVC services
builder.Services.AddEndpointsApiExplorer(); // Enables support for API endpoints
builder.Services.AddSingleton<XmlaService>();


var app = builder.Build();

var apiKey = builder.Configuration["ApiKey"]; // Retrieve API Key from appsettings.json
app.UseMiddleware<ApiKeyMiddleware>(apiKey); // Use your custom API Key middleware

app.UseHttpsRedirection(); 
app.UseAuthorization(); 

app.MapControllers(); 

app.Run(); // Run the application
