using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LoadOnTruckController : ControllerBase
    {
        private readonly ILoadOnTruckRepository _LoadOnTKService;
        public LoadOnTruckController(ILoadOnTruckRepository PreLoadService)
        {
            _LoadOnTKService = PreLoadService;
        }

        [HttpPost("GetPlanListFinishPreLoad")]
        public async Task<IActionResult> GetPlanListFinishPreLoad([FromBody] M_LoadOnTruck CLS_LOADONTRUCK)
        {
            try
            {
                var gates = await _LoadOnTKService.GetPlanListFinishPreLoadAsync(CLS_LOADONTRUCK);
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

        [HttpPost("GetPlanListLoadInTruck")]
        public async Task<IActionResult> GetPlanListLoadInTruck([FromBody] M_LoadOnTruck CLS_LOADONTRUCK)
        {
            try
            {
                var gates = await _LoadOnTKService.GetLoadInTruckPlanListAsync(CLS_LOADONTRUCK);
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

        [HttpPost("GetPlanListFinishLoad")]
        public async Task<IActionResult> GetPlanListFinishLoad([FromBody] M_LoadOnTruck CLS_LOADONTRUCK)
        {
            try
            {
                var gates = await _LoadOnTKService.getFinishLoadPlanList(CLS_LOADONTRUCK);
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

        [HttpPost("StampLoadOnTruck")]
        public async Task<IActionResult> StampLoadOnTruck([FromBody] M_LoadOnTruck CLS_ONTRUCK)
        {
            if (string.IsNullOrEmpty(CLS_ONTRUCK.DCCode))
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

                var isTrue = await _LoadOnTKService.checkTruckOnDock(CLS_ONTRUCK.PlanNo, CLS_ONTRUCK.DCCode);
                if (isTrue)
                {
                    var success = await _LoadOnTKService.StampLoadOnTruck(CLS_ONTRUCK);
                    if (!success)
                    {
                        return NotFound(new
                        {
                            success = false,
                            error = "บันทึกข้อมูล Load In Truck ไม่สำเร็จ"
                        });
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "บันทึกข้อมูล Load In Truck สำเร็จ"
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "ไม่พบรถขนส่งสินค้าที่จุดบรรจุสินค้า"
                    });
                }
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

        [HttpPost("FinishLoadOnTruck")]
        public async Task<IActionResult> FinishLoadOnTruck([FromBody] M_LoadOnTruck CLS_ONTRUCK)
        {
            if (string.IsNullOrEmpty(CLS_ONTRUCK.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับบันทึกข้อมูล"
                });
            }

            try
            {
                var success = await _LoadOnTKService.StampFinishLoadOnTruck(
                    CLS_ONTRUCK.PlanNo, CLS_ONTRUCK.LoadOnTruckFinishBy, CLS_ONTRUCK.LoadOnTruckFinishRemark, CLS_ONTRUCK.DCCode, CLS_ONTRUCK.LoadOnTruckActionBy);
                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "บันทึกข้อมูล Finish Load Truck ไม่สำเร็จ"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "บันทึกข้อมูล Finish Load Truck สำเร็จ"
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
