using Microsoft.AspNetCore.Mvc;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Server.Service;
using NamesOutOfAHat2.Service;

namespace NamesOutOfAHat2.Server.Controllers
{
    [ApiController]
    public class EmailController : ControllerBase
    {
        [HttpPost]
        [Route("api/email")]
        [Produces("application/json")]
        public async Task<IActionResult> SendAsync(
            [FromServices] ValidationService validationService,
            [FromServices] EligibilityValidationService eligibilityValidationService,
            [FromServices] EmailStagingService emailStagingService,
            [FromServices] IEmailService emailService,
            [FromBody]Hat hat
            )
        {
            var (isValid, errors) = validationService.Validate(hat);

            if (!isValid)
                return new BadRequestObjectResult(errors);

            (isValid, errors) = eligibilityValidationService.Validate(hat);

            if (!isValid)
                return new BadRequestObjectResult(errors);

            var emails = await emailStagingService.StageEmailsAsync(hat);

#if !DEBUG
            var tasks = new List<Task>();

            foreach (var email in emails)
                tasks.Add(emailService.SendAsync(email));

            await Task.WhenAll(tasks);
#endif
            return new OkResult();
        }

    }
}
