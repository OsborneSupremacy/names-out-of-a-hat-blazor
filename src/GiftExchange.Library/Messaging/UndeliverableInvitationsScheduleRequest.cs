namespace GiftExchange.Library.Messaging;

/// <summary>
/// What EventBridge Scheduler hands the function that checks, some hours after invitations went
/// out, whether any of them came back.
/// </summary>
/// <remarks>
/// The same two fields as <see cref="HatCooledOffScheduleRequest"/>, and a separate record all the
/// same. They are payloads of two different schedules aimed at two different functions, and sharing
/// one type would mean a change made for the cool-off arriving, unasked, in the shape of a
/// schedule already sitting in EventBridge waiting to fire hours from now.
///
/// The organizer's address is carried rather than looked up because every read in the provider is
/// scoped by it: it is what proves the schedule is about a hat this organizer owns, the same way
/// the cool-off transition is.
/// </remarks>
public record UndeliverableInvitationsScheduleRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }
}
