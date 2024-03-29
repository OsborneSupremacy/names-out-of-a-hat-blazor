﻿namespace NamesOutOfAHat2.Model;

public record OrganizerRegistration
{
    public Guid HatId { get; set; } = default!;

    public string OrganizerEmail { get; set; } = default!;

    public string VerificationCode { get; set; } = default!;

    public bool Verified { get; set; } = false;
}
