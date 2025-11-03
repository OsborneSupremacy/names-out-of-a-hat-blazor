using AutoFixture;
using AutoFixture.AutoMoq;

namespace NamesOutOfAHat2.Test.Utility;

public static class AutoFixtureExtensions
{
    public static IFixture AddAutoMoqCustomization(this IFixture input) =>
        input.Customize(new AutoMoqCustomization { GenerateDelegates = true, ConfigureMembers = true });
}
