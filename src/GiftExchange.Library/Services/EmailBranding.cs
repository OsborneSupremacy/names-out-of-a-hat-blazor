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
    /// <summary>The wordmark at the top of an email, linking back to the site.</summary>
    /// <remarks>
    /// See <see cref="Branding.LogoAltText"/> for why the alt text is worded the way it is; in this
    /// medium it is what most recipients actually see.
    ///
    /// The dimensions are given as attributes as well as CSS: Outlook lays out with the Word engine
    /// and honours the attributes, so leaving them off collapses the space the image should occupy.
    /// <c>height:auto</c> then stops the height attribute from squashing the image in the clients
    /// that do scale it down on a narrow screen. The asset is 960px wide for a 260px slot, which
    /// keeps it sharp on the high-density displays most mail is read on.
    /// </remarks>
    internal static string Masthead() =>
        $"""
         <a href="{Branding.SiteUrl}"><img src="{Branding.LogoUrl}" alt="{Branding.LogoAltText}" width="260" height="87" style="display:block;border:0;outline:none;text-decoration:none;max-width:100%;height:auto;" /></a>
         """;

    /// <summary>The text link every email closes on.</summary>
    internal static string SignOff() =>
        $"""<a href="{Branding.SiteUrl}"><b>{Branding.LogoAltText}</b></a>""";
}
