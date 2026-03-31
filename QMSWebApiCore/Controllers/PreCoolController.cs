using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PreCoolController : ControllerBase
    {
        private readonly IPreCoolRepository _PrecoolService;
        public PreCoolController(IPreCoolRepository PrecoolService)
        {
            _PrecoolService = PrecoolService;
        }

        [HttpPost("GetPlanListPreCool")]
        public async Task<IActionResult> GetPlanListPreCool([FromBody] M_Precool CLS_PRECOOL)
        {
            if (string.IsNullOrEmpty(CLS_PRECOOL.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับดูรายการตรวจสอบอุณหภูมิ"
                });
            }

            try
            {
                var gates = await _PrecoolService.GetPreCoolListAsync(CLS_PRECOOL);
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

        [HttpPost("StampPreCool")]  //ทำทั้ง PASS/FAIL ในเส้นเดียวกัน
        public async Task<IActionResult> StampPrecool([FromBody] M_Precool CLS_PRECOOL)
        {
            if (string.IsNullOrEmpty(CLS_PRECOOL.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับ Stamp Pro-cool"
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
                var req = await _PrecoolService.StampPreCoolAsync(CLS_PRECOOL);
                if (!req)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "Stamp ข้อมูล Pre-Cool ไม่สำเร็จ"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Stamp ข้อมูล Pre-Cool สำเร็จ"
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
        [HttpPost("DeletePreCool")]
        public async Task<IActionResult> DeletePreCool([FromBody] M_Precool CLS_PRECOOL)
        {

            if (string.IsNullOrEmpty(CLS_PRECOOL.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับ Delete Pro-cool"
                });
            }

            try
            {
                var gates = await _PrecoolService.GetPreCoolListAsync(CLS_PRECOOL);
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

        [HttpPost("CancelPreCoolPass")]
        public async Task<IActionResult> CancelPreCoolPass([FromBody] M_Precool CLS_PRECOOL)
        {
            if (string.IsNullOrEmpty(CLS_PRECOOL.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับยกเลิก การตรวจสอบอุณหภูมิผ่านแล้ว"
                });
            }

            try
            {
                var gates = await _PrecoolService.CancelPreCoolPass(CLS_PRECOOL);
                return Ok(new
                {
                    success = true,
                    data = gates
                    //count = gates.Count
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

        [HttpPost("GetPreCoolFail/{PlanNo}/{DCCode}")]
        public async Task<IActionResult> GetPreCoolFail([FromBody] M_Precool CLS_PRECOOL)
        {
            if (string.IsNullOrEmpty(CLS_PRECOOL.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับรายการไม่ผ่านตรวจสอบอุณหภูมิ"
                });
            }

            try
            {
                var gates = await _PrecoolService.GetPreCoolFailAsync(CLS_PRECOOL);
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
