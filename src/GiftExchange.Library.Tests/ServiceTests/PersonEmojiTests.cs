using System.Text.RegularExpressions;

namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// The list of faces and the rule for handing one out.
///
/// Assignment is random, so these run it repeatedly rather than once: a rule that mostly avoids a
/// collision and a rule that avoids one are the same rule as far as a single call can tell.
/// </summary>
public partial class PersonEmojiTests
{
    [Fact]
    public void EveryFaceOffered_IsDistinct()
    {
        PersonEmoji.All.Should().OnlyHaveUniqueItems();
        PersonEmoji.All.Should().NotBeEmpty();
    }

    [Fact]
    public void AnAssignedFace_IsOneOfTheOnesOffered()
    {
        foreach (var _ in Enumerable.Range(0, 100))
            PersonEmoji.All.Should().Contain(PersonEmoji.Assign([]));
    }

    /// <summary>
    /// The point of the marker. A second Alice wearing Alice's face makes the emoji beside a name
    /// say less than the name did on its own.
    /// </summary>
    [Fact]
    public void AFaceIsChosen_FromTheOnesNobodyInTheHatIsWearing()
    {
        var taken = PersonEmoji.All.Take(PersonEmoji.All.Count - 1).ToImmutableList();

        foreach (var _ in Enumerable.Range(0, 50))
            PersonEmoji.Assign(taken).Should().Be(PersonEmoji.All[^1]);
    }

    /// <summary>
    /// A hat may hold more people than there are faces, at which point repeating one is the only
    /// thing left to do. It is still a face, and it is still not always the same one.
    /// </summary>
    [Fact]
    public void WhenEveryFaceIsTaken_OneIsRepeatedRatherThanNothingReturned()
    {
        var handedOut = Enumerable
            .Range(0, 200)
            .Select(_ => PersonEmoji.Assign(PersonEmoji.All))
            .ToImmutableList();

        handedOut.Should().OnlyContain(emoji => PersonEmoji.All.Contains(emoji));
        handedOut.Distinct().Should().HaveCountGreaterThan(1, "the fallback should not always reach for the first");
    }

    /// <summary>
    /// Rows written before the column existed, and anything else that is not a face this
    /// application offers, do not use up one of the faces it does.
    /// </summary>
    [Fact]
    public void FacesThisApplicationDoesNotOffer_AreNotTreatedAsTaken()
    {
        var everyOffered = PersonEmoji.All.Concat(["", "🦕"]).ToImmutableList();

        PersonEmoji.All.Should().Contain(PersonEmoji.Assign(everyOffered));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("🦕")]
    [InlineData("<script>alert(1)</script>")]
    public void SomethingThatIsNotAnOfferedFace_IsNotOffered(string candidate) =>
        PersonEmoji.IsOffered(candidate).Should().BeFalse();

    [Fact]
    public void EveryOfferedFace_IsOffered() =>
        PersonEmoji.All.Should().OnlyContain(emoji => PersonEmoji.IsOffered(emoji));

    [Fact]
    public void ThePlaceholder_IsAFaceLikeAnyOther() =>
        PersonEmoji.IsOffered(PersonEmoji.Placeholder).Should().BeTrue();

    /// <summary>
    /// The faces are written down twice: here, and in the app's personEmoji.ts, which is what the
    /// picker offers. This side is the authority — an edit naming anything it does not know is
    /// refused by <c>EditParticipantEmojiRequestValidator</c> — so the drift is quiet and one-sided:
    /// a face the picker has and this list does not is offered to an organizer and then refused on
    /// save, which reads as the application being broken rather than as a list being out of date.
    ///
    /// Compared in order as well as by content, so the two grids read the same way round.
    /// </summary>
    [Fact]
    public void TheFacesThePickerOffers_AreTheFacesTheApplicationKnows()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "App", "personEmoji.ts"));

        var initializer = TypeScriptList().Match(source);

        initializer.Success.Should().BeTrue("personEmoji.ts should declare PERSON_EMOJI");

        var offered = TypeScriptString()
            .Matches(initializer.Groups[1].Value)
            .Select(match => match.Groups[1].Value)
            .ToImmutableList();

        // Guards the guard: a pattern that matched an empty list would agree with anything.
        offered.Should().NotBeEmpty();

        offered.Should().Equal(PersonEmoji.All);
    }

    [GeneratedRegex(@"export const PERSON_EMOJI\s*=\s*\[(.*?)\]\s*as const", RegexOptions.Singleline)]
    private static partial Regex TypeScriptList();

    [GeneratedRegex(@"'([^']+)'")]
    private static partial Regex TypeScriptString();
}
