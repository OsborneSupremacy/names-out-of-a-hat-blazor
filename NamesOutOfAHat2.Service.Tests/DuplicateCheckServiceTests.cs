using NamesOutOfAHat2.Model.DomainModels;
using NamesOutOfAHat2.Test.Utility.Services;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class DuplicateCheckServiceTests
{
    [Fact]
    public void NameDuplicateCheckService_Should_Find_Duplicates()
    {
        // arrange
        var input = new List<Person>
        {
            "Joe".ToPerson(),
            "joe".ToPerson(),
            "Sue".ToPerson(),
            "Sue  ".ToPerson(),
            "Steve".ToPerson(),
            "Stephen".ToPerson()
        };

        var service = new NameDuplicateCheckService();

        // act
        var result = service.Execute(input);

        // assert
        result.IsSuccess.Should().BeFalse();

        result.GetErrors().Count.Should().Be(2);
    }

    [Fact]
    public void NameDuplicateCheckService_Should_Not_Find_Duplicates()
    {
        // arrange
        var input = new List<Person>()
        {
            "Joe".ToPerson(),
            "Joseph".ToPerson(),
            "Sue".ToPerson(),
            "Susan".ToPerson(),
            "Steve".ToPerson(),
            "Stephen".ToPerson()
        };

        var service = new NameDuplicateCheckService();

        // act
        var result = service.Execute(input);

        // assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EmailDuplicateCheckService_Should_Find_Duplicates()
    {
        // arrange
        var input = new List<Person>()
        {
            new PersonBuilder().WithEmail("Joe@gmail.com").Build(),
            new PersonBuilder().WithEmail("Joe@gmail.com").Build(),
            new PersonBuilder().WithEmail("Sue@gmail.com").Build(),
            new PersonBuilder().WithEmail("Sue@gmail.com  ").Build(),
            new PersonBuilder().WithEmail("Steve@gmail.com").Build(),
            new PersonBuilder().WithEmail("Stephen@gmail.com").Build()
        };

        var service = new EmailDuplicateCheckService();

        // act
        var result = service.Execute(input);

        // assert
        result.IsSuccess.Should().BeFalse();
        result.GetErrors().Count.Should().Be(2);
    }

    [Fact]
    public void EmailDuplicateCheckService_Should_Not_Find_Duplicates()
    {
        // arrange
        var input = new List<Person>()
        {
            new PersonBuilder().WithEmail("Joe1@gmail.com").Build(),
            new PersonBuilder().WithEmail("Joe2@gmail.com").Build(),
            new PersonBuilder().WithEmail("Sue1@gmail.com").Build(),
            new PersonBuilder().WithEmail("Sue2@gmail.com  ").Build(),
            new PersonBuilder().WithEmail("Steve@gmail.com").Build(),
            new PersonBuilder().WithEmail("Stephen@gmail.com").Build()
        };
        var service = new EmailDuplicateCheckService();

        // act
        var isSuccess = service.Execute(input)
            .Match(
                _ => true,
                _ => false);

        // assert
        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void IdDuplicateCheckService_Should_Find_Duplicates()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        // arrange
        var input = new List<Person>()
        {
            new PersonBuilder().WithId(guid1).Build(),
            new PersonBuilder().WithId(guid1).Build(),
            new PersonBuilder().WithId(guid1).Build(),
            new PersonBuilder().WithId(guid2).Build(),
            new PersonBuilder().WithId(guid2).Build(),
            new PersonBuilder().WithId(guid2).Build()
        };

        var service = new IdDuplicateCheckService();
        List<Exception> errors = [];

        // act
        var isSuccess = service.Execute(input)
            .Match(
                _ => true,
                error =>
                {
                    if (error is AggregateException aggregateException)
                        errors = aggregateException.InnerExceptions.ToList();
                    return false;
                }
            );

        // assert
        isSuccess.Should().BeFalse();
        errors.Count.Should().Be(2);
    }

    [Fact]
    public void IdDuplicateCheckService_Should_Not_Find_Duplicates()
    {
        // arrange
        var input = new List<Person>()
        {
            new PersonBuilder().WithId(Guid.NewGuid()).Build(),
            new PersonBuilder().WithId(Guid.NewGuid()).Build(),
            new PersonBuilder().WithId(Guid.NewGuid()).Build(),
            new PersonBuilder().WithId(Guid.NewGuid()).Build(),
            new PersonBuilder().WithId(Guid.NewGuid()).Build(),
            new PersonBuilder().WithId(Guid.NewGuid()).Build()
        };

        var service = new IdDuplicateCheckService();

        // act
        var result = service.Execute(input);

        // assert
        result.IsSuccess.Should().BeTrue();
    }
}
