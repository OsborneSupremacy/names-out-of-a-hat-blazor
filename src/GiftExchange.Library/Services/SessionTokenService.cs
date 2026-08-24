using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GiftExchange.Library.Services;

/// <summary>
/// Mints and validates the session JWT. This is the token that rides on every API call; it is
/// deliberately separate from the single-use magic link token.
/// </summary>
[UsedImplicitly]
internal class SessionTokenService
{
    private const string Issuer = "https://namesoutofahat.com";

    private const string Audience = "namesoutofahat-api";

    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(14);

    private readonly SigningSecretProvider _signingSecretProvider;

    private readonly JsonWebTokenHandler _handler = new();

    // ReSharper disable once ConvertToPrimaryConstructor
    public SessionTokenService(SigningSecretProvider signingSecretProvider)
    {
        _signingSecretProvider = signingSecretProvider ?? throw new ArgumentNullException(nameof(signingSecretProvider));
    }

    public async Task<(string token, DateTimeOffset expiresAt)> IssueAsync(string email)
    {
        var key = new SymmetricSecurityKey(await _signingSecretProvider.GetSigningKeyAsync().ConfigureAwait(false));
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = DateTime.UtcNow,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Email, email)
            ]),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return (_handler.CreateToken(descriptor), expiresAt);
    }

    public async Task<(bool isValid, string email)> ValidateAsync(string token)
    {
        var key = new SymmetricSecurityKey(await _signingSecretProvider.GetSigningKeyAsync().ConfigureAwait(false));

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Pin the algorithm rather than trusting the token header, which is what closes off
            // the classic "alg" confusion attacks.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var result = await _handler
            .ValidateTokenAsync(token, parameters)
            .ConfigureAwait(false);

        if (!result.IsValid)
            return (false, string.Empty);

        return result.Claims.TryGetValue(JwtRegisteredClaimNames.Email, out var email) && email is string emailValue
            ? (true, emailValue)
            : (false, string.Empty);
    }
}
