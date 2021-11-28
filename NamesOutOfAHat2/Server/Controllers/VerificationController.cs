﻿using Microsoft.AspNetCore.Mvc;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Server.Service;
using NamesOutOfAHat2.Service;
using Bogus;

namespace NamesOutOfAHat2.Server.Controllers
{
    [ApiController]
    public class VerificationController : ControllerBase
    {
        [HttpPost]
        [Route("api/verify")]
        [Produces("application/json")]
        public async Task<IActionResult> SendAsync(
            [FromServices] VerificationEmailStagingService emailStagingService,
            [FromServices] OrganizerRegistrationService registrationService,
            [FromServices] IEmailService emailService,
            [FromBody] Hat hat
            )
        {
            var code = new Faker().Random.Int(1000, 9999).ToString();

            registrationService.Register(hat, code);
            var email = await emailStagingService.StageEmailAsync(hat, code);
            await emailService.SendAsync(email);

            return new OkResult();
        }
    }
}