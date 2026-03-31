using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TMSPlanController : ControllerBase
    {
        private readonly ITMSPlanRepository _tmsPlanRepository;

        public TMSPlanController(ITMSPlanRepository tmsPlanRepository)
        {
            _tmsPlanRepository = tmsPlanRepository;
        }

        [HttpPost("getTMSPlanListOnDate")]
        public async Task<IActionResult> GetTMSPlanListOnDate([FromBody] M_TMSPlan plans)
        {
            try
            {
                var gates = await _tmsPlanRepository.GetTMSPlanListOnDate(plans);
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

        [HttpPost("SaveTMSPlan")]
        public async Task<IActionResult> SaveTMSPlan([FromBody] List<M_TMSPlan> plans)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data format", errors = ModelState });
                }

                if (plans == null || plans.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No data provided" });
                }
                Console.WriteLine($"TMSPlan Import: Received {plans.Count} records");

                var result = await _tmsPlanRepository.SaveTMSPlanAsync(plans);
                return Ok(new { success = result, message = result ? "Data saved successfully" : "Failed to save data" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Unexpected error during TMS Plan import: {ex.Message}"
                });
            }
        }

        [HttpPost("CancelTMSPlan")]
        public async Task<IActionResult> CancelTMSPlan([FromBody] M_TMSPlan plans)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data format", errors = ModelState });
                }

                var result = await _tmsPlanRepository.CancelTMSPlan(plans);
                return Ok(new { success = result, message = result ? "Cancel TMSplan successfully" : "Failed to Cancel TMSplan data" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Unexpected error during TMS Plan import: {ex.Message}"
                });
            }
        }
    }
}
