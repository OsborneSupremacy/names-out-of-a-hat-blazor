namespace GiftExchange.Library.Models;

/// <summary>
/// How far a participant email is known to have got, as SES reported it.
///
/// The empty string is a real value here and the one most rows start at: it means nothing has been
/// heard yet, which is not the same as "not delivered". Everything that reads this has to keep the
/// two apart — an organizer told somebody has not received their invitation, when in truth no event
/// has arrived, will go and pester a person who is holding it.
/// </summary>
public static class DeliveryStatus
{
    /// <summary>Nothing has been heard about this participant. Not a status SES ever reports.</summary>
    public static string Unknown => string.Empty;

    /// <summary>SES accepted the message. It has left this application and nothing more is known.</summary>
    public static string Sent => "SENT";

    /// <summary>Temporarily undeliverable and still being retried. Neither a success nor a failure yet.</summary>
    public static string Delayed => "DELAYED";

    /// <summary>The receiving mail server accepted it. The furthest anything here can honestly claim.</summary>
    public static string Delivered => "DELIVERED";

    /// <summary>The recipient marked it as spam. Arrived, and was unwelcome.</summary>
    public static string Complained => "COMPLAINED";

    /// <summary>SES refused to send it, typically because the content tripped its virus scan.</summary>
    public static string Rejected => "REJECTED";

    /// <summary>SES could not render the message at all, so nothing was sent.</summary>
    public static string Failed => "FAILED";

    /// <summary>It came back. The one status an organizer can actually do something about.</summary>
    public static string Bounced => "BOUNCED";
}

public static class DeliveryStatuses
{
    public static readonly ImmutableList<string> All =
    [
        DeliveryStatus.Sent,
        DeliveryStatus.Delayed,
        DeliveryStatus.Delivered,
        DeliveryStatus.Complained,
        DeliveryStatus.Rejected,
        DeliveryStatus.Failed,
        DeliveryStatus.Bounced
    ];

    /// <summary>
    /// How far through the message's life each status sits, so that a row only ever moves forwards.
    /// </summary>
    /// <remarks>
    /// SES does not order event delivery, and SNS does not either. A Delivery published before a
    /// Send can be handed to us after it, and a row that took the last event to arrive rather than
    /// the furthest one reached would flap between DELIVERED and SENT for no reason anybody could
    /// see in the data.
    ///
    /// The bad outcomes rank above Delivered rather than beside it, deliberately. A complaint is
    /// something the recipient does after the message arrived, so it has to be able to overwrite
    /// the delivery it followed; a bounce outranks everything, because a message that came back is
    /// the fact worth keeping no matter what else was said about it.
    /// </remarks>
    public static int RankOf(string status) => status switch
    {
        var value when value == DeliveryStatus.Sent => 1,
        var value when value == DeliveryStatus.Delayed => 2,
        var value when value == DeliveryStatus.Delivered => 3,
        var value when value == DeliveryStatus.Complained => 4,
        var value when value == DeliveryStatus.Rejected => 5,
        var value when value == DeliveryStatus.Failed => 5,
        var value when value == DeliveryStatus.Bounced => 6,
        // Unknown, and anything SES invents after this was written. Ranking it below every real
        // status means a status we do not recognise can never displace one we do.
        _ => 0
    };
}
