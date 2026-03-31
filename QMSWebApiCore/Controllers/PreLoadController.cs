using Microsoft.AspNetCore.Mvc;
using OutboundQMSModel;
using QMSWebApiCore.Models;
using System.Net;
using static QMSWebApiCore.Services.PreLoadRepository;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PreLoadController : ControllerBase
    {
        private readonly IPreLoadRepository _PreLoadService;
        public PreLoadController(IPreLoadRepository PreLoadService)
        {
            _PreLoadService = PreLoadService;
        }

        [HttpPost("GetPlanListPreLoad")]
        public async Task<IActionResult> GetPlanListPreLoad([FromBody] M_PreLoad CLS_PRELOAD)
        {
            try
            {
                var gates = await _PreLoadService.GetPlanListPreLoad(CLS_PRELOAD);
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

        [HttpPost("GetPreLoadDetail")]
        public async Task<IActionResult> GetPreLoadDetail([FromBody] M_PreLoad CLS_PRELOAD)
        {
            try
            {
                var gates = await _PreLoadService.GetPlanListPreLoad(CLS_PRELOAD);
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
        [HttpPost("StampPreLoad")]
        public async Task<IActionResult> StampPreLoad([FromBody] M_PreLoad CLS_PreLoad)
        {
            if (string.IsNullOrEmpty(CLS_PreLoad.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับบันทึกข้อมูล"
                });
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var success = await _PreLoadService.StampPreLoad(CLS_PreLoad);
                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "บันทึกข้อมูล Pre-Load ไม่สำเร็จ"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "บันทึกข้อมูล Pre-Load สำเร็จ"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
        [HttpPost("GetPlanListFinishPreLoad")]
        public async Task<IActionResult> getFinishPreLoadPlanList([FromBody] M_FinishPreLoad CLS_FINISHPRELOAD)
        {
            try
            {
                var gates = await _PreLoadService.getFinishPreLoadPlanList(CLS_FINISHPRELOAD);
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

        [HttpPost("StampFinishPreLoad")]
        public async Task<IActionResult> StampFinishPreLoad([FromBody] M_FinishPreLoad CLS_FinishPreLoad)
        {
            if (string.IsNullOrEmpty(CLS_FinishPreLoad.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับบันทึกข้อมูล"
                });
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var success = await _PreLoadService.StampFinishPreLoad(CLS_FinishPreLoad);
                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "บันทึกข้อมูล Pre-Load ไม่สำเร็จ"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "บันทึกข้อมูล Pre-Load สำเร็จ"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}
