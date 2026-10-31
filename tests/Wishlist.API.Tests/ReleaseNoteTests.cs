using System.Text.RegularExpressions;

namespace Wishlist.API.Tests;

/// <summary>
/// Every release says what it brings: the SPA page carries a release note that
/// the "New version available" notice shows.
/// </summary>
public partial class ReleaseNoteTests
{
    [Fact]
    public void The_page_has_a_release_note()
    {
        var note = ReleaseNote().Match(File.ReadAllText(IndexHtml()));

        Assert.True(note.Success, "index.html needs <meta name=\"release-note\" content=\"…\"> describing this release.");
        Assert.False(string.IsNullOrWhiteSpace(note.Groups[1].Value), "The release note in index.html is empty.");
        Assert.True(note.Groups[1].Value.Length <= 200, "Keep the release note to 200 characters or fewer.");
    }

    [Fact]
    public void Serving_the_page_keeps_the_release_note()
    {
        var page = File.ReadAllText(IndexHtml());

        var served = SpaPage.Render(page, "https://www.kadoozle.app", "/join/abc", new SpaPage.Preview("Ann invited you", "Join"));

        Assert.Equal(ReleaseNote().Match(page).Value, ReleaseNote().Match(served).Value);
    }

    private static string IndexHtml()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Wishlist.sln"))) dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Repository root not found."),
            "src", "Wishlist.Web", "index.html");
    }

    [GeneratedRegex("""<meta\s+name="release-note"\s+content="([^"]*)"\s*/?>""")]
    private static partial Regex ReleaseNote();
}
