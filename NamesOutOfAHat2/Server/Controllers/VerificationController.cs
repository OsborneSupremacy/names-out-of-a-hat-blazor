﻿namespace NamesOutOfAHat2.Server.Controllers;

[ApiController]
public class VerificationController : ControllerBase
{
    [HttpPost]
    [Route("api/verify")]
    [Produces("application/json")]
    public IActionResult Verify(
        [FromBody] OrganizerRegistration registration,
        [FromServices] OrganizerVerificationService verificationService
        ) =>
            verificationService.Verify(registration) switch
            {
                true => new OkResult(),
                _ => new UnauthorizedResult()
            };

    [HttpPost]
    [Route("api/verify-check")]
    [Produces("application/json")]
    public IActionResult CheckIfVerified(
        [FromBody] OrganizerRegistration registration,
        [FromServices] OrganizerVerificationService verificationService
        ) =>
            verificationService.CheckVerified(registration) switch
            {
                true => new OkResult(),
                _ => new UnauthorizedResult()
            };

    [HttpPost]
    [Route("api/verify-register")]
    [Produces("application/json")]
    public async Task<IActionResult> SendAsync(
        [FromServices] VerificationEmailStagingService emailStagingService,
        [FromServices] OrganizerRegistrationService registrationService,
        [FromServices] IEmailService emailService,
        [FromBody] Hat hat
        )
    {
        var code = new Faker().Random.Int(1000, 9999).ToString();
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"Verification Code: {code}");
#endif
        registrationService.Register(hat, code);
        var email = await emailStagingService.StageEmailAsync(hat, code);

        return (await emailService.SendAsync(email))
            .Match<IActionResult>
            (
                success => new OkObjectResult(true),
                error => new BadRequestObjectResult(error)
            );
    }
}
