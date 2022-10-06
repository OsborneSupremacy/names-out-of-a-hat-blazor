namespace NamesOutOfAHat2.Server.Controllers;

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
        [FromServices] OrganizerVerificationService organizerVerificationService,
        [FromServices] IEmailService emailService,
        [FromBody] Hat hat
        )
    {
        var result = validationService.Validate(hat);

        if (!result.IsSuccess)
            return new BadRequestObjectResult(result.GetErrors());

        result = eligibilityValidationService.Validate(hat);

        if (!result.IsSuccess)
            return new BadRequestObjectResult(result.GetErrors());

        if (!organizerVerificationService.CheckVerified(hat.Id, hat.Organizer?.Person.Email ?? string.Empty))
            return new BadRequestObjectResult("Sender email address is not verfieid");

        var emails = await emailStagingService.StageEmailsAsync(hat);

        var emailErrors = new ConcurrentBag<string>();

        var tasks = new List<Task>();

        foreach(var email in emails)
            _ = (await emailService.SendAsync(email))
                .Match(
                    result =>
                    {
                        return true;
                    },
                    error =>
                    {
                        emailErrors.Add(error.Message);
                        return false;
                    }
                );

        await Task.WhenAll(tasks);

        if (emailErrors.Any())
            return new BadRequestObjectResult(emailErrors.Distinct());

        return new OkResult();
    }
}
