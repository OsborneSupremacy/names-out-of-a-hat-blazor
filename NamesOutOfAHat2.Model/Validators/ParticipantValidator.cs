namespace NamesOutOfAHat2.Model.Validators;

public class ParticipantValidator : AbstractValidator<Participant>
{
    public ParticipantValidator()
    {
        RuleFor(x => x.Person).NotNull();
        RuleFor(x => x.Recipients)
            .NotNull()
            .Must(x => x.Count >= 1)
            .WithMessage("Each participant needs at least one possible recipient.");
    }
}
