using Microsoft.AspNetCore.Mvc;
using Npgsql;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;
using System.Data;
using static QMSWebApiCore.Services.IGateInRepository;

namespace QMSWebApiCore.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class GateInController : ControllerBase
    {
        private readonly IGateInRepository _gateService;
        public GateInController(IGateInRepository gateService)
        {
            _gateService = gateService;
        }

        [HttpGet("SearchGateIn/{DCCode}/{TruckTypeID}/{StatusGateIn}")]
        public async Task<IActionResult> GetAllGates(string DCCode, string TruckTypeID, string StatusGateIn, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
        {
            try
            {
                if (string.IsNullOrEmpty(DCCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "ไม่พบ DCCode สำหรับดูรายการ Gate In ทั้งหมด"
                    });
                }

                var gates = await _gateService.GetAllGateInAsync(DCCode,TruckTypeID,StatusGateIn, startDate, endDate);
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

        [HttpGet("SearchGateInByBarcode/{Barcode}/{DCCode}")]
        public async Task<IActionResult> GetGateInByBarcode(string Barcode, string DCCode)
        {
            if (string.IsNullOrEmpty(DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับ Gate In"
                });
            }

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
                var gates = await _gateService.GatGateInByBarcodeOnceAsync(Barcode, DCCode);
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

        [HttpGet("GetBarcodeDetail/{Barcode}/{DCCode}")]  //ตัวใช้อยู่ใน EDP ใช้check barcode
        public async Task<IActionResult> GetBarcodeDetail(string Barcode, string DCCode)
        {
            if (string.IsNullOrEmpty(DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode Gate In สำหรับดูรายละเอียด Barcode"
                });
            }

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

        [HttpPost("StampGateIn")]
        public async Task<IActionResult> StampGateIn([FromBody] M_GateIn CLS_GATEIN)
        {
            if (string.IsNullOrEmpty(CLS_GATEIN.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับ Stamp Gate In"
                });
            }

            // Validation
            if (string.IsNullOrWhiteSpace(CLS_GATEIN.Barcode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Barcode is required"
                });
            }
            var gate = await _gateService.StampGateIn(CLS_GATEIN);
            if (gate == null)
            {
                return NotFound(new
                {
                    success = false,
                    error = "บันทึกรถเข้าคลังสินค้า Gate In ไม่สำเร็จ"
                });
            }
            return Ok(new
            {
                success = true,
                message = "บันทึกรถเข้าคลังสินค้า Gate In สำเร็จ"
            });
        }

        [HttpPut("UpdateGateIn")]
        public async Task<IActionResult> UpdateGateIn([FromBody] M_GateIn CLS_GATEIN)
        {
            if (string.IsNullOrEmpty(CLS_GATEIN.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับ Update Gate In"
                });
            }

            try
            {
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
                if (string.IsNullOrEmpty(CLS_GATEIN?.Barcode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "กรุณาระบุ Barcode"
                    });
                }

                var updated = await _gateService.UpdateGateInAsync(CLS_GATEIN);
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
                        barcode = CLS_GATEIN.Barcode,
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
                return StatusCode(500, new
                {
                    success = false,
                    message = "เกิดข้อผิดพลาดในการแก้ไขข้อมูล",
                    error = ex.Message
                });
            }
        }

        [HttpPost("DeleteGateIn")]
        public async Task<IActionResult> DeleteGateIn([FromBody] M_GateIn CLS_GATEIN)
        {
            if (string.IsNullOrEmpty(CLS_GATEIN.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับ Delete Gate In"
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

                var deleted = await _gateService.DeleteGateInAsync(CLS_GATEIN);
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

        [HttpGet("CheckDuplicateBarcode/{Barcode}/{DCCode}")]
        public async Task<IActionResult> CheckDuplicateGateInBarcode(string Barcode, string DCCode)
        {
            if (string.IsNullOrEmpty(DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode Gate In สำหรับตรวจสอบข้อมูลซ้ำ"
                });
            }

            try
            {
                if (string.IsNullOrEmpty(Barcode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "DCCode is required"
                    });
                }
                bool isDuplicate = await _gateService.CheckDuplicateBarcode(Barcode, DCCode);
                if (isDuplicate)
                {
                    return Ok(new
                    {
                        success = true,
                        isDuplicate = true,
                        message = $"Barcode: {Barcode} ถูกใช้งานแล้ว!"
                    });
                }

                return Ok(new
                {
                    success = true,
                    isDuplicate = false,
                    message = "Barcode สามารถใช้งานได้"
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