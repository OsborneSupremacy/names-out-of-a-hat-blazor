namespace NamesOutOfAHat2.Model.ViewModels;

public record ParticipantViewModel
{
    public required PersonViewModel Person { get; set; }

    public required PersonViewModel PickedRecipient { get; set; }

    public required List<RecipientViewModel> Recipients { get; set; }

    public static implicit operator Participant(ParticipantViewModel vm) => new Participant
    {
        Person = vm.Person,
        PickedRecipient = vm.PickedRecipient,
        Recipients = vm.Recipients.Select(rvm => (Recipient)rvm).ToList()
    };
}
