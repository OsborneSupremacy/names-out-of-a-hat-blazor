﻿namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class DuplicateCheckServiceTests
{
    [Fact]
    public void NameDuplicateCheckService_Should_Find_Duplicates()
    {
        // arrange
        var input = new List<Person>()
    {
        new Person() { Name = "Joe" },
        new Person() { Name = "joe" },
        new Person() { Name = "Sue" },
        new Person() { Name = "Sue  " },
        new Person() { Name = "Steve" },
        new Person() { Name = "Stephen" }
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
        new Person() { Name = "Joe" },
        new Person() { Name = "Joseph" },
        new Person() { Name = "Sue" },
        new Person() { Name = "Susan" },
        new Person() { Name = "Steve" },
        new Person() { Name = "Stephen" }
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
        new Person() { Email = "Joe@gmail.com" },
        new Person() { Email = "Joe@gmail.com" },
        new Person() { Email = "Sue@gmail.com" },
        new Person() { Email = "Sue@gmail.com  " },
        new Person() { Email = "Steve@gmail.com" },
        new Person() { Email = "Stephen@gmail.com" }
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
        new Person() { Email = "Joe1@gmail.com" },
        new Person() { Email = "Joe2@gmail.com" },
        new Person() { Email = "Sue1@gmail.com" },
        new Person() { Email = "Sue2@gmail.com  " },
        new Person() { Email = "Steve@gmail.com" },
        new Person() { Email = "Stephen@gmail.com" }
    };

        var service = new EmailDuplicateCheckService();

        // act
        var isSuccess = service.Execute(input)
            .Match(
                success =>
                {
                    return true;
                },
                error =>
                {
                    return false;
                }
            );

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
        new Person() { Id = guid1 },
        new Person() { Id = guid1 },
        new Person() { Id = guid1 },
        new Person() { Id = guid2 },
        new Person() { Id = guid2 },
        new Person() { Id = guid2 }
    };

        var service = new IdDuplicateCheckService();
        List<Exception> errors = null;

        // act
        var isSuccess = service.Execute(input)
            .Match(
                success =>
                {
                    return true;
                },
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
        new Person() { Id = Guid.NewGuid() },
        new Person() { Id = Guid.NewGuid() },
        new Person() { Id = Guid.NewGuid() },
        new Person() { Id = Guid.NewGuid() },
        new Person() { Id = Guid.NewGuid() },
        new Person() { Id = Guid.NewGuid() }
    };

        var service = new IdDuplicateCheckService();

        // act
        var result = service.Execute(input);

        // assert
        result.IsSuccess.Should().BeTrue();
    }
}
