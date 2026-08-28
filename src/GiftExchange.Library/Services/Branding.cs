namespace GiftExchange.Library.Services;

/// <summary>
/// Where this application lives, and where its wordmark is served from.
/// </summary>
/// <remarks>
/// Shared by the emails and by the pages the Ask renders. Those are two different media and each
/// builds its own markup — a mail client needs dimension attributes an Outlook layout engine will
/// honour, a browser page does not — so what is held here is only what must not differ.
///
/// That is the asset itself. The logo is served by the deployed front end rather than by anything
/// this project controls, so renaming or moving it breaks every caller at once, and one place to
/// change is the difference between a one-line fix and a hunt through two unrelated files.
/// </remarks>
internal static class Branding
{
    internal const string SiteUrl = "https://namesoutofahat.com";

    /// <summary>
    /// Served from the front end's public root, so the deployed site is what hosts it.
    /// </summary>
    internal const string LogoUrl = $"{SiteUrl}/logo-horizontal.png";

    /// <summary>
    /// What stands in for the wordmark when the image does not arrive.
    /// </summary>
    /// <remarks>
    /// The sign-off's wording rather than a bare product name, and that choice earns its keep
    /// mainly in email: most mail clients block remote images until the reader asks for them, so
    /// for the majority of recipients this text <em>is</em> the masthead. Written this way a
    /// blocked image degrades to the line the emails used to open with, rather than to a
    /// broken-image placeholder. The Ask pages get the same string because they are the same
    /// product and there is no reason for the two to read differently.
    /// </remarks>
    internal const string LogoAltText = "🎩 Names Out Of A Hat 🎩";
}
