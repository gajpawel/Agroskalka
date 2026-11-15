namespace Agroskalka.Services.Captcha;

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    public bool Enabled { get; set; }

    public string SiteKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string VerificationEndpoint { get; set; } =
        "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}
