namespace NamesOutOfAHat2.Model.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public record HatViewModel
{
    public required Guid Id { get; set; }

    public required List<string> Errors { get; set; }

    public required string Name { get; set; }

    public required string AdditionalInformation { get; set; }

    public required string PriceRange { get; set; }

    public required ParticipantViewModel Organizer { get; set; }

    public required List<ParticipantViewModel> Participants { get; set; }

    public static implicit operator Hat(HatViewModel vm) => new Hat
    {
        Id = vm.Id,
        Errors = new List<string>(vm.Errors),
        Name = vm.Name,
        AdditionalInformation = vm.AdditionalInformation,
        PriceRange = vm.PriceRange,
        Organizer = vm.Organizer,
        Participants = vm.Participants.Select(pvm => (Participant)pvm).ToList()
    };
}
