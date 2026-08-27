using System.Security.Cryptography;
using System.Text;

namespace GiftExchange.Library.Utility;

/// <summary>
/// Unguessable tokens, and the one-way transform they are stored under.
///
/// A token is handed out once and never held anywhere afterwards — only <see cref="Hash"/> of it
/// is, so possession of the database is not possession of the token. Matching one that comes back
/// means hashing what arrived and looking for that instead.
/// </summary>
internal static class SecretToken
{
    /// <summary>
    /// 128 bits. Enough that guessing is not a strategy, while keeping the encoded form short
    /// enough to sit in an email address a person might have to read out or retype — which is the
    /// difference between this and a token that only ever appears inside a link.
    /// </summary>
    private const int TokenBytes = 16;

    /// <summary>
    /// A new token in the clear. This is the only time it exists outside whatever it is sent in;
    /// store <see cref="Hash"/> of it rather than the return value.
    /// </summary>
    /// <remarks>
    /// Base64url rather than plain base64: it yields only letters, digits, '-' and '_', all of
    /// which an email local part may carry unquoted, where base64's '+' and '/' may not.
    /// </remarks>
    public static string Create() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    /// <summary>Hex-encoded SHA-256, which is what a token column holds.</summary>
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
