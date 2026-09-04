namespace GiftExchange.Library.Validators;

/// <summary>
/// Second line behind the JSON schema API Gateway validates against. The schema's enum already
/// rejects an unknown draw type at the edge; this is what holds if the endpoint is ever wired up
/// without its request model, and it is the only check the in-process callers pass through.
/// </summary>
public class AssignRecipientsRequestValidator : AbstractValidator<AssignRecipientsRequest>
{
    public AssignRecipientsRequestValidator()
    {
        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        RuleFor(x => x.HatId)
            .NotEmpty();

        RuleFor(x => x.DrawType)
            .NotEmpty()
            .Must(DrawTypes.All.Contains)
            .WithMessage($"'Draw Type' must be one of: {string.Join(", ", DrawTypes.All)}.");
    }
}
