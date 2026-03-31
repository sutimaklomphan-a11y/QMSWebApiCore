using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;
using System.Data;
using System.Net;
using System.Text;
using static QMSWebApiCore.Services.LoginRepository;

namespace QMSWebApiCore.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILogInRepository _LoginService;
        public LoginController(ILogInRepository LoginService)
        {
            _LoginService = LoginService;
        }

        [HttpGet("GetLogin/{AccountName}/{DCCode}")]
        public async Task<IActionResult> GetLogin(string AccountUserName, string DCCode)
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
                var gates = await _LoginService.GetLogIn(AccountUserName, DCCode);
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

        [HttpGet("getPreCoolPass")]
        public async Task<IActionResult> GetLogin([FromQuery] M_Login ClsLogin)
        {
            try
            {
                var preCoolList = await _LoginService.GetLogIn(ClsLogin.AccountUsername,ClsLogin.AccountPassword);

                if (preCoolList == null || !preCoolList.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        data = new List<object>(),
                        count = 0
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = preCoolList,
                    count = preCoolList.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }
    }
}
