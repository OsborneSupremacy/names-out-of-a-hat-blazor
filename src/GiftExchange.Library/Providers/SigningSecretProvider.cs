using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace GiftExchange.Library.Providers;

/// <summary>
/// Reads the session signing key from SSM Parameter Store. The value is cached for the life of the
/// execution environment, so the fetch happens once per cold start rather than once per request.
/// </summary>
[UsedImplicitly]
internal class SigningSecretProvider
{
    private readonly IAmazonSimpleSystemsManagement _ssmClient;

    private readonly string _parameterName;

    private readonly SemaphoreSlim _gate = new(1, 1);

    private byte[]? _cachedKey;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SigningSecretProvider(IAmazonSimpleSystemsManagement ssmClient)
    {
        _ssmClient = ssmClient ?? throw new ArgumentNullException(nameof(ssmClient));
        _parameterName = EnvReader.GetStringValue("SESSION_SIGNING_KEY_PARAMETER");
    }

    public async Task<byte[]> GetSigningKeyAsync()
    {
        if (_cachedKey is not null) return _cachedKey;

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_cachedKey is not null) return _cachedKey;

            var response = await _ssmClient
                .GetParameterAsync(new GetParameterRequest
                {
                    Name = _parameterName,
                    WithDecryption = true
                })
                .ConfigureAwait(false);

            _cachedKey = Convert.FromBase64String(response.Parameter.Value);
            return _cachedKey;
        }
        finally
        {
            _gate.Release();
        }
    }
}
