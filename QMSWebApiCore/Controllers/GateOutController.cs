using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class GateOutController : ControllerBase
    {
        private readonly IGateOutRepository _gateService;
        public GateOutController(IGateOutRepository gateService)
        {
            _gateService = gateService;
        }

        //show list gate in ที่จะ stamp gateout direct
        [HttpGet("SearchGateOutDirect/{DCCode}/{TruckTypeID}/{StatusGateOut}")]
        public async Task<IActionResult> GetGateOutDirect(string DCCode ,string TruckTypeID, string StatusGateOut)
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

                var gates = await _gateService.GetGateOutDirectAsync(DCCode,TruckTypeID, StatusGateOut);
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

        [HttpGet("SearchGateOut/{DCCode}/{TruckTypeID}/{StatusGateOut}")]
        public async Task<IActionResult> GetAllGateOut(string DCCode, string TruckTypeID, string StatusGateOut)
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
                var gates = await _gateService.GetAllGateOutAsync(DCCode, TruckTypeID, StatusGateOut);
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

        [HttpGet("SearchGateOutByBarcode/{Barcode}/{DCCode}")]
        public async Task<IActionResult> GetGateOutByBarcode(string Barcode, string DCCode)
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
                var gates = await _gateService.GatGateInByBarcodeDetail(Barcode, DCCode);
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

        //ใช้ค้นหาเพื่อ stamp gate out ที่ยื่น EDP แล้ว
        [HttpGet("getGateOutBarcodeDetail/{Barcode}/{DCCode}")]
        public async Task<IActionResult> getGateOutDetail(string Barcode, string DCCode)
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
                var gates = await _gateService.checkStatusBarcodeDetail(Barcode, DCCode);
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

        //ใช้ค้นหาเพื่อ stamp gate out Direct 
        [HttpGet("getGateOutBarcodeDirectDetail/{Barcode}/{DCCode}/{ActionDate}")]
        public async Task<IActionResult> getGateOutBarcodeDirectDetail(string Barcode, string DCCode, string ActionDate)
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
                var gates = await _gateService.GatGateOutByBarcodeDirect(Barcode, DCCode, ActionDate);
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

        [HttpPost("StampGateOut")]
        public async Task<IActionResult> StampGateOut([FromBody] M_GateOut CLS_GATEOUT)
        {
            try
            {
                // Validate ModelState
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "ข้อมูลไม่ถูกต้อง",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                // Validate Barcode specifically
                if (string.IsNullOrEmpty(CLS_GATEOUT?.Barcode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "กรุณาระบุ Barcode"
                    });
                }

                var updated = await _gateService.StampGateOutAsync(CLS_GATEOUT);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "ไม่พบข้อมูล Gate In ที่ต้องการแก้ไข หรือไม่มีการเปลี่ยนแปลงข้อมูล"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "แก้ไขรถเข้าคลังสินค้า Gate In สำเร็จ",
                    data = new
                    {
                        barcode = CLS_GATEOUT.Barcode,
                        updatedDate = DateTime.UtcNow
                    }
                });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "ข้อมูลไม่ครบถ้วน",
                    error = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (NpgsqlException ex)
            {
                // Database error
                return StatusCode(500, new
                {
                    success = false,
                    message = "เกิดข้อผิดพลาดในการเชื่อมต่อฐานข้อมูล",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                // General error
                return StatusCode(500, new
                {
                    success = false,
                    message = "เกิดข้อผิดพลาดในการแก้ไขข้อมูล",
                    error = ex.Message
                });
            }
        }

        [HttpPost("StampGateOutDirect")]
        public async Task<IActionResult> StampGateOutDirect([FromBody] M_GateOut CLS_GATEOUT)
        {
            try
            {
                // Validate ModelState
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "ข้อมูลไม่ถูกต้อง",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                // Validate Barcode specifically
                if (string.IsNullOrEmpty(CLS_GATEOUT?.Barcode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "กรุณาระบุ Barcode"
                    });
                }

                var updated = await _gateService.StampGateOutDirectAsync(CLS_GATEOUT);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "ไม่พบข้อมูล Gate In ที่ต้องการแก้ไข หรือไม่มีการเปลี่ยนแปลงข้อมูล"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "แก้ไขรถเข้าคลังสินค้า Gate In สำเร็จ",
                    data = new
                    {
                        barcode = CLS_GATEOUT.Barcode,
                        updatedDate = DateTime.UtcNow
                    }
                });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "ข้อมูลไม่ครบถ้วน",
                    error = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (NpgsqlException ex)
            {
                // Database error
                return StatusCode(500, new
                {
                    success = false,
                    message = "เกิดข้อผิดพลาดในการเชื่อมต่อฐานข้อมูล",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                // General error
                return StatusCode(500, new
                {
                    success = false,
                    message = "เกิดข้อผิดพลาดในการแก้ไขข้อมูล",
                    error = ex.Message
                });
            }
        }

        [HttpPost("DeleteGateIn")]
        public async Task<IActionResult> DeleteGateIn([FromBody] M_GateOut CLS_GATEOUT)
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

                var deleted = await _gateService.DeleteGateOutAsync(CLS_GATEOUT);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "ลบข้อมูลรถเข้าคลังสินค้า Gate In ไม่สำเร็จ"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "ลบข้อมูลรถเข้าคลังสินค้า Gate In สำเร็จ"
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
