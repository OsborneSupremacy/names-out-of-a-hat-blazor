using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Utility;

public static class HatExtensions
{
    public static OrganizerRegistration ToRegistration(this Hat input, string code) =>
        new()
        {
            HatId = input.Id,
            OrganizerEmail = input.Organizer.Person.Email,
            VerificationCode = code,
            Verified = false
        };

    public static OrganizerRegistration ToRegistration(this Hat input) =>
        input.ToRegistration(string.Empty);
}
