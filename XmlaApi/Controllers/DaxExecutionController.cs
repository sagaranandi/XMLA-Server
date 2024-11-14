using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Threading.Tasks;
using XmlaApi.Models;
using XmlaApi.Services;
using XmlaApi.DaxQueryGeneration;


namespace XmlaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DaxExecutionController : ControllerBase
    {
        private readonly XmlaService _xmlaService;
        private readonly DaxQueryBuilder _daxQueryBuilder;
        private readonly ILogger<XmlaService> _logger;

        public DaxExecutionController(XmlaService xmlaService, DaxQueryBuilder daxQueryBuilder, ILogger<XmlaService> logger)
        {
            _xmlaService = xmlaService;
            _daxQueryBuilder = daxQueryBuilder;
            _logger = logger;
        }

        [HttpPost("getData")]
        public async Task<IActionResult> ExecuteDaxQuery([FromBody] DaxAPI request)
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader) || string.IsNullOrEmpty(authorizationHeader))
            {
                return Unauthorized("Authorization header is missing or empty.");
            }

            var bearerToken = authorizationHeader.ToString().Replace("Bearer ", "");

            bool isLoaded = await _xmlaService.Load(new LoadRequest
            {
                Workspace = request.WorkspaceName,
                DatasetName = request.DatasetName
            }, bearerToken);

            if (!isLoaded)
            {
                return BadRequest("Failed to load connection.");
            }

            //  Check if DaxQuery is provided; if not, generate it from GroupBy and Aggregate
            if (string.IsNullOrEmpty(request.DaxQuery))
            {
                request.DaxQuery = _daxQueryBuilder.GenerateDaxQuery(request);
            }

            //  Execute the DAX query
            var result = await _xmlaService.Execute(new ExecuteDax { DaxQuery = request.DaxQuery });

            //  Return result
            if (result == null || result.Count == 0)
            {
                return BadRequest("Failed to execute DAX query or no results returned.");
            }

            return Ok(result);  
        }
    }
}

