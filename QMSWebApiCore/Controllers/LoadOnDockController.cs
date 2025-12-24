using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QMSWebApiCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace QMSWebApiCore.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoadOnDockController : ControllerBase
    {
       private static readonly string[] Summaries = new[]
       {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<LoadOnDockController> _logger;

        public LoadOnDockController(ILogger<LoadOnDockController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "LoadOnDock")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [Route("StampGateIn")]
        [HttpPost]
        public HttpResponseMessage StampGateIn([FromBody] M_Gate GateIn)
        {
            //clsGateIn svGateIn = new clsGateIn();
            //M_Result result = new M_Result();
            if (GateIn.Barcode == "")
            {
              
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
