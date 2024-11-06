using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using XmlaApi.Services;
using XmlaApi.Models;

namespace XmlaApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class XmlaController : ControllerBase
    {
        private readonly XmlaService _xmlaService;
        private readonly ILogger<XmlaController> _logger; 

        public XmlaController(XmlaService xmlaService, ILogger<XmlaController> logger)
        {
            _xmlaService = xmlaService;
            _logger = logger;
        }

        [HttpPost("load")]
        public IActionResult Load([FromBody] LoadRequest request)
        {
            _logger.LogInformation("Received request to load dataset: {Workspace}, {DatasetName}", request.Workspace, request.DatasetName);
        
            if (_xmlaService.Load(request))
            {
                _logger.LogInformation("Load request processed successfully.");
                return Ok("Connection successful");
            }
        
            _logger.LogWarning("Load request failed.");
            return BadRequest("Connection failed");
        }

        

        [HttpPost("execute")]
        public IActionResult Execute([FromBody] string daxQuery) 
        {
            if (string.IsNullOrEmpty(daxQuery))
            {
                return BadRequest("DAX query must be provided.");
            }

            try
            {
                var parquetPath = _xmlaService.Execute(daxQuery);
                return Ok(parquetPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the DAX query.");
                return StatusCode(500, "Internal server error. Please try again later.");
            }
        }
    }
}
