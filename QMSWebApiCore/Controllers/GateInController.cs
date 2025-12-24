// Controllers/GateInController.cs

using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;  // ← เปลี่ยนเป็น Services
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace QMSWebApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GateInController : ControllerBase
    {
        private readonly IGateRepository _gateService;  
        public GateInController(IGateRepository gateService)
        {
            _gateService = gateService;
        }

        // GET: api/gatein
        [HttpGet]
        public async Task<IActionResult> GetAllGates()
        {
            var gates = await _gateService.GetAllGateInAsync();
            return Ok(new
            {
                success = true,
                data = gates,
                count = gates.Count()
            });
        }

        // GET: api/gatein/Barcode
        [HttpGet("{Barcode}")]
        public async Task<IActionResult> GetGateInByBarcode(string Barcode)
        {
            var gate = await _gateService.GetGateInByBarcodeAsync(Barcode);

            if (gate == null)
                return NotFound(new { success = false, error = "Gate not found" });

            return Ok(new { success = true, data = gate });
        }

        // GET: api/gatein/detail
        [HttpGet("{id}/{DCCode}")]
        public async Task<IActionResult> GetGateInByDetail(int Gate_id, string DCCode)
        {
            var gate = await _gateService.GetGateInByDetailAsync(Gate_id, DCCode);

            if (gate == null)
                return NotFound(new { success = false, error = "Gate not found" });

            return Ok(new { success = true, data = gate });
        }

        // POST: Create GATE IN
        [HttpPost("StampGateIn")]
        public async Task<IActionResult> GateIn([FromBody] GateIn GT)
        {
            R_result result = new R_result();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(GT.DriverName))
                return BadRequest("Driver Name is required");

            // Optional fields - set to null if empty
            if (string.IsNullOrWhiteSpace(GT.Barcode))
                GT.Barcode = null;
            else
                GT.Barcode = GT.Barcode.Trim();

            if (string.IsNullOrWhiteSpace(GT.GateInRemark))
                GT.GateInRemark = null;
            else
                GT.GateInRemark = GT.GateInRemark.Trim();

            //Check Duplicate
            if (GT.Barcode == "")
            {
                result.Result = false;
                result.ErrorMessage = "Barcode นี้ถูกใช้แล้ว !!!";
            }

            // Save to database
            var gate = await _gateService.CreateGateInAsync(GT);

            return Ok(new { success = true, data = gate });
        }

   
        //----> update GATE IN
        [HttpPost("barcode/{barcode}")]
        public async Task<IActionResult> UpdateGateIn(string barcode, [FromBody] GateIn ClsGateIn)
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

                var updated = await _gateService.UpdateGateInAsync(barcode, ClsGateIn);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "Gate not found or no changes made"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Gate updated successfully"
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

        //[HttpPost]
        //public async Task<IActionResult> GateIn([FromBody] GateCreateDto gateDto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var gate = new GateIn
        //    {
        //        ContainerNumber = gateDto.ContainerNumber,
        //        TruckNumber = gateDto.TruckNumber,
        //        DriverName = gateDto.DriverName,
        //        SealNumber = gateDto.SealNumber,
        //        Remarks = gateDto.Remarks,
        //        Status = "IN",
        //        GateInTime = DateTime.UtcNow,
        //        CreatedAt = DateTime.UtcNow,
        //        UpdatedAt = DateTime.UtcNow
        //    };

        //    var created = await _repository.CreateAsync(gate);

        //    return CreatedAtAction(nameof(GetGateById), new { id = created.Id }, new
        //    {
        //        success = true,
        //        message = "Gate In recorded successfully",
        //        data = created
        //    });
        //}
    }
    
}