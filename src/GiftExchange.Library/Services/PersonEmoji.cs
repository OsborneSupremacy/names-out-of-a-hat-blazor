namespace GiftExchange.Library.Services;

/// <summary>
/// The faces participants are marked with, and the rule for handing one out.
/// </summary>
/// <remarks>
/// A face used to be derived from a name every time one was needed — the same name hashed to the
/// same emoji, so an invitation and the announcement that followed it agreed with each other
/// without anything being stored. That worked until an organizer wanted a say in it: a derived
/// value has nowhere to hold an edit, and renaming somebody silently changed their face.
///
/// So a face is now written against the participant when they are added, and this class is only
/// what a face is chosen from. The closed list is what makes an edit safe to accept: an organizer
/// picks one of these rather than typing anything, so nothing here needs moderating, escaping, or
/// checking for length beyond the column it goes into.
/// </remarks>
internal static class PersonEmoji
{
    /// <summary>
    /// What is offered, and the only values the application will store.
    /// </summary>
    /// <remarks>
    /// Cheerful faces, grouped so the picker reads as something arranged rather than poured out.
    /// The order within a group is arbitrary and the count is not load-bearing — a hat with more
    /// participants than there are faces gives two people the same one, which is untidy rather than
    /// wrong. More of them is still better than fewer: <see cref="Assign"/> can only keep a hat's
    /// faces distinct while it has unused ones to reach for.
    ///
    /// What is left out is the part worth writing down, because a face here is not decoration in
    /// general — it is assigned to a named person and shown beside their name, in the participant
    /// list and in the email telling somebody they drew them. So the test each candidate has to
    /// pass is whether it could be read as a remark about the person wearing it:
    ///
    /// <list type="bullet">
    /// <item>Nothing unhappy, unwell or cross. A face is somebody's marker for the length of an
    /// exchange, and there is no version of this feature that is improved by assigning somebody a
    /// crying one.</item>
    /// <item>No caricature — the clown, the nerd and the disguise are jokes at the expense of
    /// whoever they land on.</item>
    /// <item>Nothing amorous or intoxicated. The kisses, the money mouth and the woozy face all
    /// say something about a person that this application has no business saying.</item>
    /// <item>No monkeys. The three wise monkeys are innocent enough on their own, and a monkey
    /// beside a person's name carries a history the other faces do not.</item>
    /// <item>Single code points only. The faces built out of zero-width joiners — the one exhaling,
    /// the one in clouds — fall apart into two unrelated emoji in the mail clients that do not
    /// know them, and every one of these has to survive being posted into an email.</item>
    /// </list>
    /// </remarks>
    public static readonly ImmutableList<string> All =
    [
        // Grins and laughs
        "😀",
        "😃",
        "😄",
        "😁",
        "😆",
        "😅",
        "🤣",
        "😂",

        // Smiles and warmth
        "🙂",
        "🙃",
        "😉",
        "😊",
        "😌",
        "😇",
        "🥰",
        "😍",
        "🤩",

        // Playful
        "😋",
        "😛",
        "😜",
        "🤪",
        "😝",
        "🤗",
        "🤭",
        "🤫",
        "😏",

        // Costumes and characters
        "🤠",
        "🥳",
        "😎",
        "🤖",
        "👽",
        "👾",
        "👻",

        // Cats
        "😺",
        "😸",
        "😹",
        "😻",
        "😼",

        // Sun and moon
        "🌝",
        "🌞",
        "🌛",
        "🌜"
    ];

    /// <summary>
    /// What stands in for a real participant's face where there is no real participant — the
    /// invitation preview, which is composed for an organizer looking at a message addressed to
    /// nobody in particular.
    /// </summary>
    public static string Placeholder => All[0];

    /// <summary>
    /// A face for somebody joining a hat, preferring one nobody in it already has.
    /// </summary>
    /// <remarks>
    /// Distinctness is the whole point of the marker: two people wearing the same face in the same
    /// exchange makes the emoji beside a name say less than the name did on its own. It is a
    /// preference rather than a guarantee, because a hat may hold more people than there are faces
    /// — at which point repeating one is the only thing left to do, and is done at random rather
    /// than by always reaching for the first.
    /// </remarks>
    /// <param name="taken">The faces already worn in this hat. Anything not in <see cref="All"/> is
    /// ignored, which covers the rows written before the column existed.</param>
    public static string Assign(IEnumerable<string> taken)
    {
        var unused = All.Except(taken).ToImmutableList();

        var pool = unused.IsEmpty ? All : unused;

        return pool[Random.Shared.Next(pool.Count)];
    }

    /// <summary>
    /// Whether this is one of the faces on offer. What an edit is checked against — the column is
    /// not free text, and the request that fills it is the one place somebody could try to make it
    /// so.
    /// </summary>
    public static bool IsOffered(string emoji) => All.Contains(emoji);
}
