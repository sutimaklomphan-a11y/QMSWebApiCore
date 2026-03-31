using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;
using System;
using System.Data;
using System.Net;
using static QMSWebApiCore.Services.EDPRepository;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EDPInController : ControllerBase
    {
        private readonly IEDPRepository _EDPService;

        public EDPInController(IEDPRepository EDPService)
        {
            _EDPService = EDPService;
        }

        [HttpPost("GetPlanListEDP")]
        public async Task<IActionResult> GetPlanListEDP([FromBody] M_EDP CLS_EDP)
        {
            try
            {
                if(CLS_EDP.EDPStatusID == "All")
                {
                    var gates = await _EDPService.getFinshLoadForEDPPlanList(CLS_EDP);
                    return Ok(new
                    {
                        success = true,
                        data = gates,
                        count = gates.Count
                    });
                }
                else
                {
                    var gates = await _EDPService.getEDPForEDPPlanList(CLS_EDP);
                    return Ok(new
                    {
                        success = true,
                        data = gates,
                        count = gates.Count
                    });
                }
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

        // GET: api/EDP
        [HttpGet("SearchEDPInByBarcode/{DCcode}/{Barcode}/{ActionDate}")]
        public async Task<IActionResult> SearchEDPInByBarcode(string DCcode, string barcode, string ActionDate)
        {
            try
            {
                if (string.IsNullOrEmpty(DCcode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "DCCode is required"
                    });
                }
                var gates = await _EDPService.checkStatusBarcodeDetail(DCcode, barcode, ActionDate);
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

        [HttpPost("StampEDPIn")]
        public async Task<IActionResult> StampEDPIn([FromBody] M_EDP CLS_EDP)
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

                var deleted = await _EDPService.StampEDPInAsync(CLS_EDP);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "ยื่นเอกสาร EPD In ไม่สำเร็จ"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "ยื่นเอกสาร EPD In สำเร็จ"
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

        [HttpPost("StampEDPOut")]
        public async Task<IActionResult> StampEDPOut([FromBody] M_EDP CLS_EDP)
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

                var deleted = await _EDPService.StampEDPOutAsync(CLS_EDP);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "ยื่นเอกสาร EDP Out ไม่สำเร็จ"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "ยื่นเอกสาร EDP Out สำเร็จ"
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
