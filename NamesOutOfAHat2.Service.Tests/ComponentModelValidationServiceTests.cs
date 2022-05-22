using FluentAssertions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class ComponentModelValidationServiceTests
{
    [Fact]
    public void Validate_Should_Fail_When_One_Item_Invalid()
    {
        // arrange
        var items = new List<TestItem>()
        {
            new TestItem(),
            new TestItem() { TestValue = "Hello"}
        };

        var service = new ComponentModelValidationService();

        // act
        var (isValid, results) = service.ValidateList(items);

        // assert
        isValid.Should().BeFalse();
        results.Count.Should().Be(1);
    }

    [Fact]
    public void Validate_Should_Pass_When_All_Items_Valid()
    {
        // arrange
        var items = new List<TestItem>()
        {
            new TestItem() { TestValue = "Hola" },
            new TestItem() { TestValue = "Hello"}
        };

        var service = new ComponentModelValidationService();

        // act
        var (isValid, _) = service.ValidateList(items);

        // assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_No_Items()
    {
        // arrange
        var items = new List<TestItem>();
        var service = new ComponentModelValidationService();

        // act
        var (isValid, _) = service.ValidateList(items);

        // assert
        isValid.Should().BeTrue();
    }

    public record TestItem
    {
        [Required(AllowEmptyStrings = false)]
        public string TestValue { get; init; } = default!;
    }
}
