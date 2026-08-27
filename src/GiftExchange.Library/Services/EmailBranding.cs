namespace GiftExchange.Library.Services;

/// <summary>
/// The masthead and sign-off every outgoing email carries.
/// </summary>
/// <remarks>
/// One place, because the sign-off was previously written out twice and the two copies had to stay
/// character-for-character identical to read as the same product.
/// </remarks>
internal static class EmailBranding
{
    private const string SiteUrl = "https://namesoutofahat.com";

    /// <summary>
    /// Served from the front end's public root, so the deployed site is what hosts it.
    /// </summary>
    private const string LogoUrl = $"{SiteUrl}/logo-horizontal.png";

    /// <summary>The wordmark at the top of an email, linking back to the site.</summary>
    /// <remarks>
    /// The alt text is the sign-off's wording rather than a bare product name, because most mail
    /// clients block remote images until the reader asks for them and the alt text is what the
    /// majority of recipients will actually see. Written that way, a blocked image degrades to the
    /// same linked line the emails used to open with instead of to a broken-image placeholder.
    ///
    /// The dimensions are given as attributes as well as CSS: Outlook lays out with the Word engine
    /// and honours the attributes, so leaving them off collapses the space the image should occupy.
    /// <c>height:auto</c> then stops the height attribute from squashing the image in the clients
    /// that do scale it down on a narrow screen. The asset is 960px wide for a 260px slot, which
    /// keeps it sharp on the high-density displays most mail is read on.
    /// </remarks>
    internal static string Masthead() =>
        $"""
         <a href="{SiteUrl}"><img src="{LogoUrl}" alt="🎩 Names Out Of A Hat 🎩" width="260" height="87" style="display:block;border:0;outline:none;text-decoration:none;max-width:100%;height:auto;" /></a>
         """;

    /// <summary>The text link every email closes on.</summary>
    internal static string SignOff() =>
        $"""<a href="{SiteUrl}"><b>🎩 Names Out Of A Hat 🎩</b></a>""";
}
