namespace GiftExchange.Library.Models;

/// <summary>
/// What a piece of feedback from the contact form is about.
///
/// Strings rather than an enum, for the reason <see cref="EmailMessageType"/> is: these cross the
/// wire in both directions and are read by a human in a notification. An enum would serialize as
/// an integer under the source-generated serializer, and the schema API Gateway validates against
/// would then describe a number nobody can read in the SNS message that results.
/// </summary>
public static class FeedbackCategory
{
    public static string Question => "QUESTION";

    public static string FeatureRequest => "FEATURE_REQUEST";

    public static string OtherFeedback => "OTHER_FEEDBACK";
}

public static class FeedbackCategories
{
    /// <summary>
    /// The categories the form offers, and the only ones the endpoint accepts. Adding one here is
    /// not enough on its own — the JSON schema enum and the frontend's list are the other two
    /// places, and <c>SchemaDriftTests</c> keeps this list and the schema together.
    /// </summary>
    public static readonly ImmutableList<string> All =
    [
        FeedbackCategory.Question,
        FeedbackCategory.FeatureRequest,
        FeedbackCategory.OtherFeedback
    ];

    /// <summary>
    /// How a category reads in the notification. Kept beside the categories rather than in the
    /// service, so that adding one and forgetting to label it fails to compile rather than
    /// arriving in an inbox spelled OTHER_FEEDBACK.
    /// </summary>
    public static string Describe(string category) => category switch
    {
        var value when value == FeedbackCategory.Question => "Question",
        var value when value == FeedbackCategory.FeatureRequest => "Feature request",
        var value when value == FeedbackCategory.OtherFeedback => "Other feedback",
        _ => category
    };
}
