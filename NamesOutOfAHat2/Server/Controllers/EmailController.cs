using Microsoft.AspNetCore.Mvc;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Server.Controllers
{
    [ApiController]
    public class EmailController : ControllerBase
    {
        [HttpPost]
        [Route("api/email/getsomestuff")]
        [Produces("application/json")]
        public async Task<IActionResult> SendAsync(
            [FromServices] IEmailService emailService,
            [FromBody]Hat hat)
        {


            return new OkResult();
        }

    }
}
