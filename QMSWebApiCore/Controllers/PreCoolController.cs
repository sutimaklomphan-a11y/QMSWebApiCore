using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Services;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PreCoolController : ControllerBase
    {
        private readonly IGateInRepository _gateService;
        public PreCoolController(IGateInRepository gateService)
        {
            _gateService = gateService;
        }
        [HttpGet("SearchPreCool/{DCCode}")]
        public async Task<IActionResult> SearchPreCoolAll(string DCCode)
        {
            try
            {
                if (string.IsNullOrEmpty(DCCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "DCCode is required"
                    });
                }
                var gates = await _gateService.GetAllGateInAsync(DCCode);
                return Ok(new
                {
                    success = true,
                    data = gates,
                    count = gates.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}
