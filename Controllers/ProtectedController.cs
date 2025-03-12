using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication22.Controllers
{
    [Route("api/protected")]
    [ApiController]
    public class ProtectedController : ControllerBase
    {
        [HttpGet]
        [Authorize] 
        public IActionResult GetSecretData()
        {
            return Ok(new { message = "Приватные данные пользователя:" });
        }
    }
}
