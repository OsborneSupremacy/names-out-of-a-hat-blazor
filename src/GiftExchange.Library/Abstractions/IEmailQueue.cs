namespace GiftExchange.Library.Abstractions;

/// <summary>
/// Where an email addressed to a participant is handed off to be sent.
/// </summary>
internal interface IEmailQueue
{
    public Task EnqueueAsync(GiftExchangeEmailRequest email);
}
