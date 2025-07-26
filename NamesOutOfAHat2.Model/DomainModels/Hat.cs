using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model.DomainModels;

public record Hat
{
    [Required]
    public required Guid Id { get; init; }

    public required List<string> Errors { get; init; }

    public required string Name { get; init; }

    [MaxLength(10000)]
    public required string AdditionalInformation { get; init; }

    [MaxLength(255)]
    public required string PriceRange { get; init; }

    public required Participant Organizer { get; init; }

    [Required,
        MinLength(3, ErrorMessage = "A gift exchange like this needs at least three people"),
        MaxLength(30, ErrorMessage = "30 is the maximum number of gift exchange participants.")
    ]
    public required List<Participant> Participants { get; init; }

    public static implicit operator HatViewModel(Hat hat) => new HatViewModel
    {
        Id = hat.Id,
        Errors = new List<string>(hat.Errors),
        Name = hat.Name,
        AdditionalInformation = hat.AdditionalInformation,
        PriceRange = hat.PriceRange,
        Organizer = hat.Organizer,
        Participants = hat.Participants.Select(p => (ParticipantViewModel)p).ToList()
    };
}

public static class Hats
{
    public static Hat Empty => new()
    {
        Id = Guid.Empty,
        Errors = [],
        Name = string.Empty,
        AdditionalInformation = string.Empty,
        PriceRange = string.Empty,
        Organizer = Participants.Empty,
        Participants = []
    };
}

