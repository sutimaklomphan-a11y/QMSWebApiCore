using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Services;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportQMSRepository _ReportService;
        public ReportController(IReportQMSRepository ReportService)
        {
            _ReportService = ReportService;
        }

        [HttpGet("GetReportGateIn/{DCCode}/{StatusGateIn}")]
        public async Task<IActionResult> GetReportGateIn(string DCCode, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
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
                var gates = await _ReportService.GetReportGateIn(DCCode, startDate, endDate);
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

        [HttpGet("getReportRSUIn/{DCCode}")]
        public async Task<IActionResult> getReportRSUIn(string DCCode, [FromQuery] string? startDate, [FromQuery] string? endDate)
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
                var gates = await _ReportService.GetReportRSUIn(DCCode, startDate, endDate);
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
        
        [HttpGet("getReportRSUOut/{DCCode}")]
        public async Task<IActionResult> getReportRSUOut(string DCCode, [FromQuery] string? startDate, [FromQuery] string? endDate)
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
                var gates = await _ReportService.GetReportRSUOut(DCCode, startDate, endDate);
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
        [HttpGet("getReportPreCool/{DCCode}")]
        public async Task<IActionResult> getReportPreCool(string DCCode, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
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
                var gates = await _ReportService.GetReportPrecool(DCCode, startDate, endDate);
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
        [HttpGet("getReportPreCoolShowStore/{DCCode}")]
        public async Task<IActionResult> getReportPreCoolShowStore(string DCCode, string Status, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
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
                var gates = await _ReportService.GetReportPreCoolShowStore(DCCode, Status, startDate, endDate);
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
        [HttpGet("getReportTruckOnDock/{DCCode}")]
        public async Task<IActionResult> getReportTruckOnDock(string DCCode, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
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
                var gates = await _ReportService.GetReportTruckOnDock(DCCode, startDate, endDate);
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
        [HttpGet("getReportPreLoad/{DCCode}")]
        public async Task<IActionResult> getReportPreLoad(string DCCode, string Status , [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
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
                var gates = await _ReportService.GetReportPreLoad(DCCode, Status, startDate, endDate);
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
        [HttpGet("getReportLoad/{DCCode}")]
        public async Task<IActionResult> getReportLoad(string DCCode, string Status, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
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
                var gates = await _ReportService.GetReportLoad(DCCode, Status, startDate, endDate);
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
        [HttpGet("getReportEDP/{DCCode}")]
        public async Task<IActionResult> getReportEDP(string DCCode, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
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
                var gates = await _ReportService.GetReportEDP(DCCode, startDate, endDate);
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
        [HttpGet("getReportSummary/{DCCode}")]
        public async Task<IActionResult> getReportSummary(string DCCode, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
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
                var gates = await _ReportService.GetReportSummary(DCCode, startDate, endDate);
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
        [HttpGet("getReportGateOut/{DCCode}")]
        public async Task<IActionResult> getReportGateOut(string DCCode, [FromQuery] string? startDate, [FromQuery] string? endDate)
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
                var gates = await _ReportService.GetReportGateOut(DCCode, startDate, endDate);
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
