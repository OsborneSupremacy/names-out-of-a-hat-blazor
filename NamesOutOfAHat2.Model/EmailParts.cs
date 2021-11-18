
namespace NamesOutOfAHat2.Model
{
    public record EmailParts
    {
        public string SenderEmail { get; init; } = default!;

        public string RecipientEmail { get; init; } = default!;

        public string Subject { get; init; } = default!;

        public string PlainTextBody { get; init; } = default!;

        public string HtmlBody { get; init; } = default!;
    }
}
