namespace Wishlist.Infrastructure.Options;

/// <summary>
/// SMTP connection details and the default From identity. Bound from the
/// <c>Smtp:</c> configuration section.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string? User { get; set; }
    public string? Password { get; set; }
    public bool UseStartTls { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
