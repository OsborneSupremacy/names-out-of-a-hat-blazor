using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Test.Utility.Services;

public class HatBuilder
{
    private readonly List<Participant> _participants;

    private Participant? _organizer;

    private Guid? _id;

    private readonly string _name;

    private readonly string _priceRange;

    private readonly string _additionalInformation;

    public HatBuilder()
    {
        _id = Guid.NewGuid();
        _participants = [];
        _organizer = null;
        _name = string.Empty;
        _priceRange = string.Empty;
        _additionalInformation = string.Empty;
    }

    public HatBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public HatBuilder WithOrganizer(Participant organizer)
    {
        _organizer = organizer;
        return this;
    }

    public HatBuilder WithParticipant(Participant participant)
    {
        _participants.Add(participant);
        return this;
    }

    public Hat Build() =>
        new()
        {
            Id = _id ?? Guid.NewGuid(),
            Organizer = null,
            Errors = [],
            Name = _name,
            AdditionalInformation = _additionalInformation,
            PriceRange = _priceRange,
            Participants = _participants
        };
}
