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
        [Route("api/emailpreview")]
        [Produces("application/json")]
        [ProducesResponseType(200)]
        public ActionResult<string> GetEmailPreview(
            [FromServices] ValidationService validationService,
            [FromServices] EmailStagingService emailStagingService,
            [FromBody] Hat hat
            )
        {
            var (isValid, errors) = validationService.Validate(hat);

            if (!isValid)
                return new BadRequestObjectResult(errors);

            var email = emailStagingService.GenerateEmail(hat);

            return new OkObjectResult(email);
        }

        [HttpPost]
        [Route("api/email")]
        [Produces("application/json")]
        public async Task<IActionResult> SendAsync(
            [FromServices] ValidationService validationService,
            [FromServices] EligibilityValidationService eligibilityValidationService,
            [FromServices] EmailStagingService emailStagingService,
            [FromServices] OrganizerVerificationService organizerVerificationService,
            [FromServices] IEmailService emailService,
            [FromBody] Hat hat
            )
        {
            var (isValid, errors) = validationService.Validate(hat);

            if (!isValid)
                return new BadRequestObjectResult(errors);

            (isValid, errors) = eligibilityValidationService.Validate(hat);

            if (!isValid)
                return new BadRequestObjectResult(errors);

            if(!organizerVerificationService.CheckVerified(hat.Id, hat.Organizer?.Person.Email ?? string.Empty))
                return new BadRequestObjectResult(errors);

            var emails = await emailStagingService.StageEmailsAsync(hat);

            var emailErrors = new List<string>();

            var tasks = new List<Task>();

            emails.ForEach(email => {
                tasks.Add(
                    emailService.SendAsync(email)
                        .ContinueWith(async (task) => {
                            var (success, details) = await task;
                            if (!success)
                                emailErrors.Add(details);
                        })
                );
            });

            await Task.WhenAll(tasks);

            if (emailErrors.Any())
                return new BadRequestObjectResult(emailErrors);

            return new OkResult();
        }
    }
}
