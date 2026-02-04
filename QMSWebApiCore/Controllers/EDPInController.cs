using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;

namespace QMSWebApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EDPInController : ControllerBase
    {
        private readonly IGateInRepository _gateService;

        public EDPInController(IGateInRepository gateService)
        {
            _gateService = gateService;

        }

        // GET: api/gatein
        [HttpGet("SearchEDPInByBarcode/{DCcode}/{Barcode}/{ActionDate}")]
        public async Task<IActionResult> SearchEDPInByBarcode(string DCcode, string barcode, string ActionDate)
        {
            var gates = await _gateService.SearchEDPInByBarcode(DCcode, barcode, ActionDate);
            return Ok(new
            {
                success = true,
                data = gates,
                //count = gates.Count()
            });
        }
    }
}
