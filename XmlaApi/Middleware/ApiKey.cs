//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging; // Add this using directive
//using System.IO; // Needed for StreamReader
//using System.Text; // Needed for Encoding
//using System.Threading.Tasks;

//namespace XmlaApi.Middleware 
//{
//    public class ApiKeyMiddleware
//    {
//        private const string API_KEY_NAME = "X-Api-Key"; // Header name for the API key
//        private readonly RequestDelegate _next;
//        private readonly string _apiKey;
//        private readonly ILogger<ApiKeyMiddleware> _logger; // Add logger field

//        public ApiKeyMiddleware(RequestDelegate next, string apiKey, ILogger<ApiKeyMiddleware> logger) // Add logger parameter
//        {
//            _next = next;
//            _apiKey = apiKey; // The expected API key from configuration
//            _logger = logger; // Initialize logger
//        }

//        public async Task InvokeAsync(HttpContext context)
//        {
//            context.Request.EnableBuffering(); // Enable buffering so we can read the body
//            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
//            {
//                var body = await reader.ReadToEndAsync();
//                context.Request.Body.Position = 0; // Reset the body stream position so next middleware can read it
//                // Log the body
//                _logger.LogInformation($"Request Body: {body}");
//            }

//            // Check if the API key is in the request headers
//            if (!context.Request.Headers.TryGetValue(API_KEY_NAME, out var extractedApiKey))
//            {
//                context.Response.StatusCode = 401; // Unauthorized
//                await context.Response.WriteAsync("API Key was not provided.");
//                return;
//            }

//            // Validate the API key
//            if (!string.Equals(extractedApiKey, _apiKey))
//            {
//                context.Response.StatusCode = 401; // Unauthorized
//                await context.Response.WriteAsync("Unauthorized client.");
//                return;
//            }

//            // If the API key is valid, call the next middleware
//            await _next(context);
//        }
//    }
//}
