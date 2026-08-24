namespace GiftExchange.Library.Abstractions;

/// <summary>
/// A request scoped to one organizer's data. <see cref="Utility.ApiGatewayAdapter"/> overwrites
/// <see cref="OrganizerEmail"/> with the authenticated caller before the request reaches a service,
/// so a client cannot name someone else's mailbox and read or modify their gift exchanges.
/// </summary>
internal interface IOrganizerScopedRequest
{
    string OrganizerEmail { get; init; }

    IOrganizerScopedRequest WithOrganizerEmail(string organizerEmail);
}
