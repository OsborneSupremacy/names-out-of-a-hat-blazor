namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record SubmitFeedbackRequest : IOrganizerScopedRequest
{
    /// <summary>
    /// Filled in by <see cref="Utility.ApiGatewayAdapter"/> from the authorizer. The form has no
    /// address field at all: the footer that opens it only renders on signed-in pages, so the
    /// sender is already known, and a field would only invite someone to mistype it or to put
    /// somebody else's address in the reply line.
    /// </summary>
    public string OrganizerEmail { get; init; } = string.Empty;

    /// <summary>One of <see cref="Models.FeedbackCategories.All"/>.</summary>
    public required string Category { get; init; }

    public required string Message { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
