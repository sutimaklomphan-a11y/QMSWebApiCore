using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RSUInController : ControllerBase
    {
        private readonly IRSUInRepository _RSUService;
        public RSUInController(IRSUInRepository RSUService)
        {
            _RSUService = RSUService;
        }

        [HttpGet("SearchRSUInByBarcode/{Barcode}/{DCCode}")]
        public async Task<IActionResult> SearchRSUInByBarcode(string Barcode, string DCCode)
        {
            if (string.IsNullOrEmpty(DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับดูการค้นหา"
                });
            }

            try
            {
                var gates = await _RSUService.SearchRSUInByBarcode(Barcode, DCCode);
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

        [HttpGet("SearchRSUIn/{TruckTypeID}/{StatusRSU}/{DCCode}")]
        public async Task<IActionResult> SearchRSUIn(string TruckTypeID,string StatusRSU, string DCCode)
        {
            if (string.IsNullOrEmpty(DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับดูข้อมูล RSU"
                });
            }

            try
            {
                var gates = await _RSUService.SearchRSUIn(TruckTypeID, StatusRSU,DCCode);
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

        [HttpPost("StampRSUIn")]   //บันทักรายการคืนสินทรัพย์ RSU In
        public async Task<IActionResult> StampRSUIn([FromBody] M_RSUInOut CLS_RSU)
        {
            if (string.IsNullOrEmpty(CLS_RSU.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับ Stamp RSU In"
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

                var duplicate = await _RSUService.chkDupRSUInBarcode(CLS_RSU.GateID);
                if (!duplicate )
                {
                    var req = await _RSUService.StampRSUIn(CLS_RSU);
                    if (!req)
                    {
                        return NotFound(new
                        {
                            success = false,
                            error = "Stamp ข้อมูล RSU In ไม่สำเร็จ"
                        });
                    }
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "Dupplicate ไม่สามารถเพิ่มข้อมูลได้"
                    });
                }
                return Ok(new
                {
                    success = true,
                    message = "Stamp ข้อมูล RSU In สำเร็จ"
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
        
        [HttpPost("StampRSUOut")]   //บันทักรายการคืนสินทรัพย์เรียบร้อยแล้ว RSU Out
        public async Task<IActionResult> StampRSUOut([FromBody] M_RSUInOut CLS_RSU)
        {
            if (string.IsNullOrEmpty(CLS_RSU.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode สำหรับ Stamp RSU Out"
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

                var deleted = await _RSUService.StampRSUOut(CLS_RSU);
                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "Stamp ข้อมูล RSU Out ไม่สำเร็จ"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Stamp ข้อมูล RSU Out สำเร็จ"
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

        [HttpPost("DeleteRSUIn")]  //บันทักรายการwไม่คืนสินทรัพย์ RSU Not Return
        public async Task<IActionResult> DeleteRSUIn([FromBody] M_RSUInOut CLS_RSU)
        {
            if (string.IsNullOrEmpty(CLS_RSU.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode กรุณาตรวจสอบอีกครั้ง"
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

                var deleted = await _RSUService.DeleteRSUIn(CLS_RSU);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "ลบข้อมูล RSU In ไม่สำเร็จ"
                    });
                }
                return Ok(new
                {
                    success = true,
                    message = "ลบข้อมูล RSU In สำเร็จ"
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

        [HttpPost("StampRSUInOut")]  //บันทักรายการwไม่คืนสินทรัพย์ RSU Not Return
        public async Task<IActionResult> StampRSUInOut([FromBody] M_RSUInOut CLS_RSU)
        {
            if (string.IsNullOrEmpty(CLS_RSU.DCCode))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ไม่พบ DCCode กรุณาตรวจสอบอีกครั้ง"
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

                var duplicate = await _RSUService.chkDupRSUInBarcode(CLS_RSU.GateID);
                if (!duplicate)
                {
                    var req = await _RSUService.StampRSUInOut(CLS_RSU);
                    if (!req)
                    {
                        return NotFound(new
                        {
                            success = false,
                            error = "Stamp ข้อมูล RSU Out ไม่สำเร็จ"
                        });
                    }
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "Dupplicate ไม่สามารถเพิ่มข้อมูลได้"
                    });
                }
                return Ok(new
                {
                    success = true,
                    message = "Stamp ข้อมูล RSU Out สำเร็จ"
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
