namespace Agroskalka.Services.Captcha;

public interface ICaptchaService
{
    bool IsEnabled { get; }

    string SiteKey { get; }

    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken);
}
