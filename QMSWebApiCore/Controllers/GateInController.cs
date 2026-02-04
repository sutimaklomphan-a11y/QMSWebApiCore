using Microsoft.AspNetCore.Mvc;
using Npgsql;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        [HttpGet("SearchGateIn/{DCCode}")]
        public async Task<IActionResult> GetAllGates(string DCCode)
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

        [HttpGet("SearchGateInByBarcode/{Barcode}/{DCCode}")]
        public async Task<IActionResult> GetGateInByBarcode(string Barcode, string DCCode)
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

        [HttpPost("StampGateIn")]
        public async Task<IActionResult> GateIn([FromBody] M_GateIn ClsGateIn)
        {
            // เช็ค Duplicate
            string duplicateBarcode = await _gateService.CheckDuplicateBarcode(ClsGateIn.Barcode);

            if (!string.IsNullOrEmpty(duplicateBarcode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Barcode {duplicateBarcode} ถูกใช้งานแล้ว!",
                    isDuplicate = true
                });
            }
            else ClsGateIn.Barcode = ClsGateIn.Barcode.Trim();

            var gate = await _gateService.CreateGateInAsync(ClsGateIn);
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
        public async Task<IActionResult> UpdateGateIn([FromBody] M_GateIn ClsGateIn)
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
                if (string.IsNullOrEmpty(ClsGateIn?.Barcode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "กรุณาระบุ Barcode"
                    });
                }

                var updated = await _gateService.UpdateGateInAsync(ClsGateIn);

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
                        barcode = ClsGateIn.Barcode,
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
        public async Task<IActionResult> DeleteGateIn([FromBody] M_GateIn ClsGateIn)
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

                var deleted = await _gateService.DeleteGateInAsync(ClsGateIn);

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