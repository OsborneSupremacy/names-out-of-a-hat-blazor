namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// The shaker is randomized, so most of these run it repeatedly and assert something that has to
/// hold every time rather than something that happens to hold once. A property that only holds on
/// some seeds is not a property, and a draw rule that only usually applies is worse than no rule at
/// all — the organizer who chose it would never know which kind of draw they got.
/// </summary>
public class HatShakerServiceTests
{
    /// <summary>
    /// Enough runs that a rule which held by luck rather than by construction would be caught, and
    /// few enough that the suite does not notice. The two-cycle test below is the sensitive one: on
    /// four people with nobody excluded, a third of draws contain a mutual pair, so a rule that did
    /// not really forbid them survives this many runs with probability under 1 in 10^8.
    /// </summary>
    private const int Runs = 50;

    [Theory]
    [InlineData("ANYTHING_GOES")]
    [InlineData("NO_MUTUAL_PAIRS")]
    [InlineData("SINGLE_CYCLE")]
    public void Shake_WhateverTheDrawType_GivesEverybodyExactlyOneNameAndNobodyTheirOwn(string drawType)
    {
        var participants = EverybodyEligible(6);

        for (var run = 0; run < Runs; run++)
        {
            // act
            var response = Shake(participants, drawType);

            // assert
            response.Success.Should().BeTrue();
            response.Participants.Should().HaveCount(6);

            var givers = response.Participants.Select(participant => participant.Person.Name);
            var recipients = response.Participants.Select(participant => participant.PickedRecipient);

            givers.Should().OnlyHaveUniqueItems();
            recipients.Should().OnlyHaveUniqueItems();
            recipients.Should().BeEquivalentTo(givers, "everyone in the hat is drawn exactly once");

            response.Participants
                .Should()
                .NotContain(participant => participant.PickedRecipient == participant.Person.Name);
        }
    }

    /// <summary>
    /// The behaviour that prompted the setting. Anything goes really does mean anything, so the
    /// mutual pairs the other two rule out have to be reachable here — otherwise the three options
    /// would be a choice between two things.
    /// </summary>
    [Fact]
    public void Shake_AnythingGoes_AllowsTwoPeopleToDrawEachOther()
    {
        var participants = EverybodyEligible(4);

        var sawAMutualPair = Enumerable
            .Range(0, Runs)
            .Select(_ => Shake(participants, DrawType.AnythingGoes))
            .Any(response => MutualPairs(response.Participants).Any());

        sawAMutualPair.Should().BeTrue();
    }

    [Fact]
    public void Shake_NoMutualPairs_NeverLetsTwoPeopleDrawEachOther()
    {
        var participants = EverybodyEligible(4);

        for (var run = 0; run < Runs; run++)
        {
            // act
            var response = Shake(participants, DrawType.NoMutualPairs);

            // assert
            response.Success.Should().BeTrue();
            MutualPairs(response.Participants).Should().BeEmpty();
        }
    }

    [Fact]
    public void Shake_SingleCycle_PutsEverybodyInOneUnbrokenChain()
    {
        var participants = EverybodyEligible(8);

        for (var run = 0; run < Runs; run++)
        {
            // act
            var response = Shake(participants, DrawType.SingleCycle);

            // assert
            response.Success.Should().BeTrue();
            CycleLengths(response.Participants).Should().Equal(8);
        }
    }

    /// <summary>
    /// The point the setting's copy makes: a draw type is applied on top of the exclusions, never
    /// instead of them.
    /// </summary>
    [Theory]
    [InlineData("ANYTHING_GOES")]
    [InlineData("NO_MUTUAL_PAIRS")]
    [InlineData("SINGLE_CYCLE")]
    public void Shake_WhateverTheDrawType_StillHonoursTheExclusions(string drawType)
    {
        // Two couples and two others. Neither half of a couple may draw the other, which is the
        // exclusion nearly every real exchange has.
        var participants = ImmutableList.Create(
            Participant("Ana", "Ben", "Chi", "Dev", "Eli", "Fay"),
            Participant("Ben", "Ana", "Chi", "Dev", "Eli", "Fay"),
            Participant("Chi", "Ana", "Ben", "Eli", "Fay"),
            Participant("Dev", "Ana", "Ben", "Eli", "Fay"),
            Participant("Eli", "Ana", "Ben", "Chi", "Dev"),
            Participant("Fay", "Ana", "Ben", "Chi", "Dev")
        );

        var eligibilityByName = participants.ToDictionary(
            participant => participant.Person.Name,
            participant => participant.EligibleRecipients
        );

        for (var run = 0; run < Runs; run++)
        {
            // act
            var response = Shake(participants, drawType);

            // assert
            response.Success.Should().BeTrue();

            foreach (var participant in response.Participants)
                eligibilityByName[participant.Person.Name]
                    .Should()
                    .Contain(
                        participant.PickedRecipient,
                        "{0} may only draw somebody they are allowed to draw",
                        participant.Person.Name
                    );
        }
    }

    /// <summary>
    /// The warning the shake dialog carries, demonstrated. These exclusions have exactly one
    /// solution and that solution is two mutual pairs, so the two constrained draw types have
    /// nowhere to go — not because the shaker gave up early, but because nothing satisfies both.
    /// </summary>
    [Fact]
    public void Shake_WhenTheOnlySolutionIsMutualPairs_OnlyAnythingGoesCanFindOne()
    {
        var participants = ImmutableList.Create(
            Participant("Ana", "Ben"),
            Participant("Ben", "Ana"),
            Participant("Chi", "Dev"),
            Participant("Dev", "Chi")
        );

        Shake(participants, DrawType.AnythingGoes).Success.Should().BeTrue();
        Shake(participants, DrawType.NoMutualPairs).Success.Should().BeFalse();
        Shake(participants, DrawType.SingleCycle).Success.Should().BeFalse();
    }

    /// <summary>
    /// A failed shake hands back nothing rather than a half-filled hat, so a caller that assigns
    /// first and checks afterwards cannot write a partial draw to the database.
    /// </summary>
    [Fact]
    public void Shake_WhenItCannotFindADraw_ReturnsNoParticipants()
    {
        var participants = ImmutableList.Create(
            Participant("Ana", "Ben"),
            Participant("Ben", "Ana"),
            Participant("Chi", "Dev"),
            Participant("Dev", "Chi")
        );

        // act
        var response = Shake(participants, DrawType.SingleCycle);

        // assert
        response.Success.Should().BeFalse();
        response.Participants.Should().BeEmpty();
    }

    /// <summary>
    /// The tightest satisfiable case: exclusions that leave exactly one chain. Every draw type has
    /// to find it, and single cycle has to find it without the retries mattering.
    /// </summary>
    [Theory]
    [InlineData("ANYTHING_GOES")]
    [InlineData("NO_MUTUAL_PAIRS")]
    [InlineData("SINGLE_CYCLE")]
    public void Shake_WhenExactlyOneDrawIsPossible_FindsIt(string drawType)
    {
        var participants = ImmutableList.Create(
            Participant("Ana", "Ben"),
            Participant("Ben", "Chi"),
            Participant("Chi", "Dev"),
            Participant("Dev", "Ana")
        );

        // act
        var response = Shake(participants, drawType);

        // assert
        response.Success.Should().BeTrue();
        response.Participants
            .OrderBy(participant => participant.Person.Name)
            .Select(participant => participant.PickedRecipient)
            .Should()
            .Equal("Ben", "Chi", "Dev", "Ana");
    }

    /// <summary>
    /// A re-shake arrives with the previous draw still on the records. Nothing should survive it.
    /// </summary>
    [Fact]
    public void Shake_GivenParticipantsWhoAlreadyDrew_IgnoresWhatTheyDrewBefore()
    {
        var participants = EverybodyEligible(5)
            .Select(participant => participant with { PickedRecipient = "Stale" })
            .ToImmutableList();

        // act
        var response = Shake(participants, DrawType.AnythingGoes);

        // assert
        response.Success.Should().BeTrue();
        response.Participants.Should().NotContain(participant => participant.PickedRecipient == "Stale");
    }

    private static ShakeHatResponse Shake(ImmutableList<Participant> participants, string drawType) =>
        HatShakerService.Shake(new ShakeHatRequest
        {
            Participants = participants,
            DrawType = drawType,
            // The production number for a constrained draw. The unsatisfiable cases below are
            // asserting that no seed works, so they need the shaker to have genuinely run out of
            // seeds rather than out of patience.
            Attempts = 250
        });

    private static Participant Participant(string name, params string[] eligibleRecipients) =>
        Participants.Empty with
        {
            Person = new Person { Name = name, Email = $"{name.ToLowerInvariant()}@example.com" },
            EligibleRecipients = [.. eligibleRecipients]
        };

    /// <summary>A hat in which anybody may draw anybody but themselves.</summary>
    private static ImmutableList<Participant> EverybodyEligible(int count)
    {
        var names = Enumerable.Range(0, count).Select(index => $"Person{index}").ToList();

        return names
            .Select(name => Participant(name, [.. names.Where(other => other != name)]))
            .ToImmutableList();
    }

    /// <summary>Every pair who drew each other.</summary>
    private static ImmutableList<string> MutualPairs(ImmutableList<Participant> participants)
    {
        var pickedByGiver = participants.ToDictionary(
            participant => participant.Person.Name,
            participant => participant.PickedRecipient
        );

        return participants
            .Where(participant =>
                pickedByGiver.TryGetValue(participant.PickedRecipient, out var theirPick)
                && theirPick == participant.Person.Name)
            .Select(participant => $"{participant.Person.Name} and {participant.PickedRecipient}")
            .ToImmutableList();
    }

    /// <summary>
    /// The lengths of the cycles the draw decomposes into, longest first. One entry equal to the
    /// number of participants is a single cycle; a 2 anywhere in it is a mutual pair.
    /// </summary>
    private static ImmutableList<int> CycleLengths(ImmutableList<Participant> participants)
    {
        var pickedByGiver = participants.ToDictionary(
            participant => participant.Person.Name,
            participant => participant.PickedRecipient
        );

        var visited = new HashSet<string>();
        var lengths = new List<int>();

        foreach (var participant in participants)
        {
            if (!visited.Add(participant.Person.Name))
                continue;

            var length = 1;
            var current = pickedByGiver[participant.Person.Name];

            while (visited.Add(current))
            {
                length++;
                current = pickedByGiver[current];
            }

            lengths.Add(length);
        }

        return [.. lengths.OrderByDescending(length => length)];
    }
}
