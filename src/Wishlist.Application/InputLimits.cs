using Wishlist.Domain.Exceptions;

namespace Wishlist.Application;

/// <summary>
/// The longest text people can store. Checked before anything is saved, so an
/// overlong value gives a 400 with a clear message instead of a database error.
/// Values match the database columns where they exist; the SPA uses the same
/// numbers as maxLength.
/// </summary>
public static class InputLimits
{
    public const int DisplayName = 100;
    public const int Email = 256;
    public const int Password = 256;
    public const int CircleName = 200;
    public const int ItemTitle = 300;
    public const int ItemDescription = 5000;
    public const int Url = 2048;

    /// <summary>Throws a 400 when <paramref name="value"/> is longer than <paramref name="max"/>.</summary>
    public static void Check(string? value, int max, string label)
    {
        if (value is not null && value.Length > max)
            throw new DomainException($"{label} can be at most {max} characters.");
    }
}
