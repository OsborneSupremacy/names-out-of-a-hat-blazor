namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record UpdateProfileRequest : IOrganizerScopedRequest
{
    /// <summary>
    /// Filled in by <see cref="Utility.ApiGatewayAdapter"/> from the authorizer, so unlike the
    /// older requests this one is not required on the wire and clients do not send it.
    /// </summary>
    public string OrganizerEmail { get; init; } = string.Empty;

    public required string Name { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
