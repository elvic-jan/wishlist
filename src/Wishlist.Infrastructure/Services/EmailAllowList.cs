namespace Wishlist.Infrastructure.Services;

/// <summary>
/// Who a test environment may email. Entries are <c>name@domain</c> (that address,
/// and <c>name+tag@domain</c>) or <c>*@domain</c> (anyone at that domain), compared
/// without regard to case. An empty list allows no one.
/// </summary>
public sealed class EmailAllowList
{
    private readonly HashSet<string> _addresses;
    private readonly HashSet<string> _domains;

    private EmailAllowList(HashSet<string> addresses, HashSet<string> domains, string? firstPlainAddress)
    {
        _addresses = addresses;
        _domains = domains;
        FirstPlainAddress = firstPlainAddress;
    }

    /// <summary>The first exact entry without a <c>+tag</c>: the inbox test users get <c>+tag</c> addresses of.</summary>
    public string? FirstPlainAddress { get; }

    public bool IsEmpty => _addresses.Count == 0 && _domains.Count == 0;

    public static EmailAllowList Parse(string? value)
    {
        var addresses = new HashSet<string>(StringComparer.Ordinal);
        var domains = new HashSet<string>(StringComparer.Ordinal);
        string? firstPlain = null;

        foreach (var raw in (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var entry = raw.ToLowerInvariant();
            if (!TrySplit(entry, out var local, out var domain)) continue;
            if (local == "*")
            {
                domains.Add(domain);
                continue;
            }
            addresses.Add(entry);
            if (firstPlain is null && !local.Contains('+')) firstPlain = entry;
        }

        return new EmailAllowList(addresses, domains, firstPlain);
    }

    public bool Allows(string recipient)
    {
        var address = recipient.Trim().ToLowerInvariant();
        if (!TrySplit(address, out var local, out var domain)) return false;
        if (_domains.Contains(domain) || _addresses.Contains(address)) return true;

        var plus = local.IndexOf('+');
        return plus > 0 && _addresses.Contains($"{local[..plus]}@{domain}");
    }

    /// <summary>
    /// For logs: <c>you+alice@gmail.com</c> → <c>y**+alice@g****.com</c>. The <c>+tag</c>
    /// (which test account) and the top-level domain stay readable; the rest doesn't.
    /// </summary>
    public static string Mask(string address)
    {
        if (!TrySplit(address.Trim(), out var local, out var domain)) return "***";

        var plus = local.IndexOf('+');
        var name = plus >= 0 ? local[..plus] : local;
        var tag = plus >= 0 ? local[plus..] : string.Empty;

        var dot = domain.LastIndexOf('.');
        var host = dot > 0 ? domain[..dot] : domain;
        var tld = dot > 0 ? domain[dot..] : string.Empty;

        return $"{Hide(name)}{tag}@{Hide(host)}{tld}";
    }

    private static string Hide(string part) =>
        part.Length == 0 ? part : part[0] + new string('*', part.Length - 1);

    private static bool TrySplit(string address, out string local, out string domain)
    {
        var at = address.LastIndexOf('@');
        local = at > 0 ? address[..at] : string.Empty;
        domain = at > 0 ? address[(at + 1)..] : string.Empty;
        return at > 0 && domain.Length > 0;
    }
}
