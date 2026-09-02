namespace GiftExchange.Library.Tests.ServiceTests;

public class PriceRangePhrasingTests
{
    [Theory]
    // A range, which is what the box asks for and the only shape the old wording handled.
    [InlineData("$25 - $40", "Please purchase a gift costing $25 - $40.")]
    [InlineData("$20-$50", "Please purchase a gift costing $20-$50.")]
    [InlineData("20 to 30 dollars", "Please purchase a gift costing 20 to 30 dollars.")]
    // A single figure. "in the range of $100" was never a range.
    [InlineData("$100", "Please purchase a gift costing $100.")]
    [InlineData("£20", "Please purchase a gift costing £20.")]
    [InlineData("25 euros", "Please purchase a gift costing 25 euros.")]
    // A figure behind a qualifier. This is the shape that produced "in the range of around $100."
    [InlineData("around $100", "Please purchase a gift costing around $100.")]
    [InlineData("under $25", "Please purchase a gift costing under $25.")]
    [InlineData("up to $50", "Please purchase a gift costing up to $50.")]
    [InlineData("no more than $50", "Please purchase a gift costing no more than $50.")]
    [InlineData("at least 10 dollars", "Please purchase a gift costing at least 10 dollars.")]
    [InlineData("between $20 and $50", "Please purchase a gift costing between $20 and $50.")]
    [InlineData("approximately 30 GBP", "Please purchase a gift costing approximately 30 GBP.")]
    // A currency named before its figure rather than after it.
    [InlineData("USD 25", "Please purchase a gift costing USD 25.")]
    public void Describe_GivenAnAmount_BuildsTheSentenceAroundIt(string priceRange, string expected)
    {
        // act
        var sentence = PriceRangePhrasing.Describe(priceRange);

        // assert
        sentence.Should().Be(expected);
    }

    [Theory]
    // An organizer writing a line rather than filling in a box. Wrapping any of these in "Please
    // purchase a gift costing ..." would read worse than what they wrote.
    [InlineData("Keep it under $20", "Keep it under $20.")]
    [InlineData("Anything up to 30 dollars", "Anything up to 30 dollars.")]
    [InlineData("Spend what you like", "Spend what you like.")]
    // No figure at all, so there is no amount to build a sentence around.
    [InlineData("no limit", "No limit.")]
    [InlineData("whatever you like", "Whatever you like.")]
    public void Describe_GivenTheOrganizersOwnWords_KeepsThemAndOnlyPunctuates(
        string priceRange,
        string expected)
    {
        // act
        var sentence = PriceRangePhrasing.Describe(priceRange);

        // assert
        sentence.Should().Be(expected);
    }

    [Theory]
    // A full stop the organizer typed themselves should not decide the shape, and should not end up
    // doubled.
    [InlineData("$25 - $40.", "Please purchase a gift costing $25 - $40.")]
    [InlineData("around $100.", "Please purchase a gift costing around $100.")]
    [InlineData("Keep it under $20.", "Keep it under $20.")]
    // Nor should the case they happened to type a qualifier in.
    [InlineData("Around $100", "Please purchase a gift costing around $100.")]
    [InlineData("Under $25", "Please purchase a gift costing under $25.")]
    // Surrounding space is theirs to get wrong.
    [InlineData("  $25 - $40  ", "Please purchase a gift costing $25 - $40.")]
    public void Describe_TidiesWhatTheOrganizerTyped(string priceRange, string expected)
    {
        // act
        var sentence = PriceRangePhrasing.Describe(priceRange);

        // assert
        sentence.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Describe_GivenNoPrice_SaysNothing(string priceRange)
    {
        // act
        var sentence = PriceRangePhrasing.Describe(priceRange);

        // assert: the caller drops the line entirely rather than printing an empty paragraph.
        sentence.Should().BeEmpty();
    }
}
