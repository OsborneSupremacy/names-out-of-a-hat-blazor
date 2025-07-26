using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Test.Utility.Services;

public class PersonBuilder
{
    private Guid _id;

    private string _name;

    private string _email;

    public PersonBuilder()
    {
        _id = Guid.NewGuid();
        _name = string.Empty;
        _email = string.Empty;
    }

    public PersonBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public PersonBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PersonBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public Person Build() =>
        new()
        {
            Id = _id,
            Name = _name,
            Email = _email
        };
}
