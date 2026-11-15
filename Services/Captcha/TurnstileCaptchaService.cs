using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Agroskalka.Services.Captcha;

public sealed class TurnstileCaptchaService : ICaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly CaptchaOptions _options;
    private readonly ILogger<TurnstileCaptchaService> _logger;

    public TurnstileCaptchaService(
        HttpClient httpClient,
        IOptions<CaptchaOptions> options,
        ILogger<TurnstileCaptchaService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.Enabled;

    public string SiteKey => _options.SiteKey;

    public async Task<bool> VerifyAsync(
        string? token,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return true;

        if (string.IsNullOrWhiteSpace(_options.SecretKey) ||
            string.IsNullOrWhiteSpace(_options.VerificationEndpoint))
        {
            _logger.LogError("CAPTCHA is enabled but is not fully configured.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("CAPTCHA verification was submitted without a token.");
            return false;
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                _options.VerificationEndpoint,
                new
                {
                    secret = _options.SecretKey,
                    response = token,
                    remoteip = remoteIp
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CAPTCHA verification endpoint returned HTTP status {StatusCode}.",
                    response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerificationResponse>(
                cancellationToken);

            if (result?.Success != true)
            {
                _logger.LogWarning(
                    "CAPTCHA verification failed with codes: {ErrorCodes}.",
                    result?.ErrorCodes is { Count: > 0 }
                        ? string.Join(", ", result.ErrorCodes)
                        : "none");
                return false;
            }

            return true;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "CAPTCHA verification request failed.");
            return false;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogError(exception, "CAPTCHA verification request timed out.");
            return false;
        }
    }

    private sealed class TurnstileVerificationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("error-codes")]
        public IReadOnlyCollection<string>? ErrorCodes { get; init; }
    }
}
