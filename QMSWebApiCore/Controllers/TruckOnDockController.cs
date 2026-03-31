using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TruckOnDockController : ControllerBase
    {
        private readonly ITruckOnDockRepository _TruckonDockService;
        public TruckOnDockController(ITruckOnDockRepository TruckonDockService)
        {
            _TruckonDockService = TruckonDockService;
        }

        [HttpPost("GetPlanListTruckOnDock")]
        public async Task<IActionResult> GetPlanListTruckOnDock([FromBody] M_TruckOnDock CLS_TRUCKONDOCK)
        {
            try
            {
                var gates = await _TruckonDockService.GetPlanListTruckOnDock(CLS_TRUCKONDOCK);
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

        [HttpPost("StampTruckOnDock")]
        public async Task<IActionResult> StampTruckOnDock([FromBody] M_TruckOnDock CLS_TRUCKONDOCK)
        {
            if (string.IsNullOrEmpty(CLS_TRUCKONDOCK.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับดูข้อมูล Truck On Dock"
                });
            }
            var isDuplicate = await _TruckonDockService.TruckOnDockDuplicateBarcode( CLS_TRUCKONDOCK.Barcode, CLS_TRUCKONDOCK.ActionDate, CLS_TRUCKONDOCK.DCCode);
            if (isDuplicate == true)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Barcode นี้อยู่ในสถานะพร้อมที่จุดขนส่งสินค้าแล้ว"
                });
            }
            else
            {
                var _TruckOnDock = await _TruckonDockService.StampTruckOnDock(CLS_TRUCKONDOCK);
                if (!_TruckOnDock)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "บันทึกข้อมูลลงฐานข้อมูลไม่สำเร็จ"
                    });
                }
                return Ok(new
                {
                    success = true,
                });
            }
        }
        [HttpPost("DeleteGateIn")]
        public async Task<IActionResult> DeleteGateIn([FromBody] M_TruckOnDock CLS_TRUCKONDOCK)
        {
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
                var deleted = await _TruckonDockService.DeleteTruckOnDock(CLS_TRUCKONDOCK);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "ลบข้อมูลรถพร้อมที่จุดบรรจุสินค้า Truck On Dock ไม่สำเร็จ"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "ลบข้อมูลรถรถพร้อมที่จุดบรรจุสินค้า Truck On Dock สำเร็จ"
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
