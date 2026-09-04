namespace GiftExchange.Library.Messaging;

/// <summary>
/// A request for the whole of one gift exchange, as data rather than as a page.
/// </summary>
/// <remarks>
/// The same two fields as <see cref="GetHatRequest"/>, and deliberately a separate type: the export
/// is organizer-scoped in the way the adapter understands, so the address it carries is overwritten
/// with the authenticated caller before the service ever sees it.
/// </remarks>
internal record ExportHatRequest : IOrganizerScopedRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
