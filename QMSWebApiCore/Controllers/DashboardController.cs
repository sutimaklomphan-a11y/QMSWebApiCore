using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OutboundQMSModel;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardQMSRepository _DashbaoardRepository;

        public DashboardController(IDashboardQMSRepository dashboardRepository)
        {
            _DashbaoardRepository = dashboardRepository;
        }

        [HttpGet("GetTimeLine/{ActionDate}/{status}/{BigCType}/{PlanGroupNo}/{DCCode}")]
        public async Task<IActionResult> GetTimeLine(string ActionDate, string Status, string BigCType, string PlanGroupNo, string DCCode)
        {
            try
            {
                if (string.IsNullOrEmpty(DCCode))
                {
                    return BadRequest(new { success = false, error = "DCCode is required" });
                }
                var gates = await _DashbaoardRepository.GetTimeLine(ActionDate, Status, BigCType, PlanGroupNo, DCCode);
                return Ok(new { success = true, data = gates, count = gates.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("GetDashboard/{ActionDate}/{BigCType}/{GroupNo}/{DCCode}")]
        public async Task<IActionResult> GetDashboard(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                if (string.IsNullOrEmpty(DCCode))
                {
                    return BadRequest(new { success = false, error = "DCCode is required" });
                }
                var gates = await _DashbaoardRepository.GetTimeLine(ActionDate,null, BigCType, GroupNo, DCCode);
                return Ok(new { success = true, data = gates, count = gates.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("getAllCountPlan")]
        public async Task<IActionResult> GetAllCountPlanPost([FromBody] M_Dashboard? data)
        {
            try
            {
                string rawBody = "N/A"; 
                try { rawBody = JsonSerializer.Serialize(data); } catch { }
                Console.WriteLine($"[DashboardController] Request received: {rawBody}");

                if (data == null)
                {
                    return BadRequest(new { success = false, error = "Request body is required or could not be parsed." });
                }

                if (string.IsNullOrEmpty(data.DCCode))
                {
                    Console.WriteLine("[DashboardController] Error: DCCode is missing in the request.");
                    return BadRequest(new
                    {
                        success = false,
                        error = "DCCode is required",
                        receivedData = data
                    });
                }

                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                {
                    data.PlanActionDate = result.ToString("yyyy-MM-dd");
                }

                var gates = await _DashbaoardRepository.GetAllCountPlan(data.PlanActionDate ?? "", data.PlanBigCType ?? "", data.PlanGroupNo ?? "", data.DCCode);
                return Ok(new
                {
                    success = true,
                    data = gates,
                    count = gates?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardController] Critical Error: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("PostStackBarFinishLoadTime")]
        public async Task<IActionResult> PostStackBarFinishLoadTime([FromBody] M_Dashboard? data)
        {
            try
            {
                string rawBody = "N/A";
                try { rawBody = JsonSerializer.Serialize(data); } catch { }
                Console.WriteLine($"[DashboardController] Request received: {rawBody}");

                if (data == null)
                {
                    return BadRequest(new { success = false, error = "Request body is required or could not be parsed." });
                }

                if (string.IsNullOrEmpty(data.DCCode))
                {
                    Console.WriteLine("[DashboardController] Error: DCCode is missing in the request.");
                    return BadRequest(new
                    {
                        success = false,
                        error = "DCCode is required",
                        receivedData = data
                    });
                }

                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                {
                    data.PlanActionDate = result.ToString("yyyy-MM-dd");
                }

                var gates = await _DashbaoardRepository.getStackBarFinishLoadTime(data.PlanActionDate ?? "", data.PlanBigCType ?? "", data.PlanGroupNo ?? "", data.DCCode);
                return Ok(new
                {
                    success = true,
                    data = gates,
                    count = gates?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardController] Critical Error: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        [HttpPost("PostStackBarFinish")]
        public async Task<IActionResult> PostStackBarFinish([FromBody] M_Dashboard? data)
        {
            try
            {
                if (data == null) return BadRequest(new { success = false, error = "Data is required" });

                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime actionDate))
                {
                    data.PlanActionDate = actionDate.ToString("yyyy-MM-dd");
                }

                var results = await _DashbaoardRepository.getStackBarFinish(data.PlanActionDate, data.PlanBigCType, data.PlanGroupNo, data.DCCode);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("PostStackBarRemain")]
        public async Task<IActionResult> PostStackBarRemain([FromBody] M_Dashboard? data)
        {
            try
            {
                if (data == null) return BadRequest(new { success = false, error = "Data is required" });

                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime actionDate))
                {
                    data.PlanActionDate = actionDate.ToString("yyyy-MM-dd");
                }

                var results = await _DashbaoardRepository.getStackBarRemain(data.PlanActionDate, data.PlanBigCType, data.PlanGroupNo, data.DCCode);
                return Ok(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardController] PostStackBarRemain Error: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("GetPlanListRemainFinish")]
        public async Task<IActionResult> GetPlanListRemainFinish([FromBody] M_Dashboard? data)
        {
            try
            {
                if (data == null) return BadRequest(new { success = false, error = "Data is required" });
                
                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime actionDate))
                {
                    data.PlanActionDate = actionDate.ToString("yyyy-MM-dd");
                }

                var result = await _DashbaoardRepository.getPlanRemain(data.PlanActionDate, data.PlanBigCType, data.PlanGroupNo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("GetChartPreCool")]
        public async Task<IActionResult> PostChartPreCool([FromBody] M_Dashboard? data)
        {
            try
            {
                if (data == null) return BadRequest(new { success = false, error = "Data is required" });
                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime actionDate))
                {
                    data.PlanActionDate = actionDate.ToString("yyyy-MM-dd");
                }
                var results = await _DashbaoardRepository.getCountPlanPreCool(data.PlanActionDate, data.PlanBigCType, data.PlanGroupNo, data.DCCode);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("GetChartTruckOnDock")]
        public async Task<IActionResult> PostChartTruckOnDock([FromBody] M_Dashboard? data)
        {
            try
            {
                if (data == null) return BadRequest(new { success = false, error = "Data is required" });
                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime actionDate))
                {
                    data.PlanActionDate = actionDate.ToString("yyyy-MM-dd");
                }
                var results = await _DashbaoardRepository.getCountPlanTruckOnDock(data.PlanActionDate, data.PlanBigCType, data.PlanGroupNo, data.DCCode);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("GetChartStartLoad")]
        public async Task<IActionResult> PostChartStartLoad([FromBody] M_Dashboard? data)
        {
            try
            {
                if (data == null) return BadRequest(new { success = false, error = "Data is required" });
                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime actionDate))
                {
                    data.PlanActionDate = actionDate.ToString("yyyy-MM-dd");
                }
                var results = await _DashbaoardRepository.getCountPlanStartLoad(data.PlanActionDate, data.PlanBigCType, data.PlanGroupNo, data.DCCode);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("GetChartFinishLoad")]
        public async Task<IActionResult> PostChartFinishLoad([FromBody] M_Dashboard? data)
        {
            try
            {
                if (data == null) return BadRequest(new { success = false, error = "Data is required" });
                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime actionDate))
                {
                    data.PlanActionDate = actionDate.ToString("yyyy-MM-dd");
                }
                var results = await _DashbaoardRepository.getCountPlanFinishLoad(data.PlanActionDate, data.PlanBigCType, data.PlanGroupNo, data.DCCode);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("GetChartEDP")]
        public async Task<IActionResult> PostChartEDP([FromBody] M_Dashboard? data)
        {
            try
            {
                if (data == null) return BadRequest(new { success = false, error = "Data is required" });
                if (DateTime.TryParseExact(data.PlanActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime actionDate))
                {
                    data.PlanActionDate = actionDate.ToString("yyyy-MM-dd");
                }
                var results = await _DashbaoardRepository.getCountPlanEDP(data.PlanActionDate, data.PlanBigCType, data.PlanGroupNo, data.DCCode);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("GetChartFinishLoad_Old/{ActionDate}/{BigCType}/{DCCode}")]
        public async Task<IActionResult> GetChartFinishLoad(string ActionDate, string BigCType, string DCCode)
        {
            try
            {
                if (DateTime.TryParseExact(ActionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime actionDate))
                {
                    ActionDate = actionDate.ToString("yyyy-MM-dd");
                }

                var results = await _DashbaoardRepository.getDashboardChartFinishLoad(ActionDate, BigCType, DCCode);
                if (results.Any())
                {
                    var data = results[0];
                    // Return as array of numbers for Chart.js [Remain, Success]
                    return Ok(new int[] { int.Parse(data.PlanRemain ?? "0"), int.Parse(data.PlanFinish ?? "0") });
                }
                return Ok(new int[] { 0, 0 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}
