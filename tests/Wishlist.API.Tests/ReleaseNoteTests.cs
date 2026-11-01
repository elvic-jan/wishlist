using System.Text.RegularExpressions;

namespace Wishlist.API.Tests;

/// <summary>
/// Release note files (release-notes/*.md) are read by the deploy
/// (scripts/release-version.sh) and shown on the What's new page, so a broken
/// one fails here instead of in a release.
/// </summary>
public partial class ReleaseNoteTests
{
    public static TheoryData<string> NoteFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.GetFiles(NotesDir(), "*.md").Where(f => Path.GetFileName(f) != "README.md").Order())
            data.Add(Path.GetFileName(file));
        return data;
    }

    [Theory]
    [MemberData(nameof(NoteFiles))]
    public void Note_file_is_well_formed(string name)
    {
        var text = File.ReadAllText(Path.Combine(NotesDir(), name)).ReplaceLineEndings("\n");
        var match = NoteFile().Match(text);

        Assert.True(match.Success, $"{name}: start with a '---' header holding 'type: feature', 'type: fix' or 'type: major', then '---' and the note.");
        var lines = match.Groups["body"].Value.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        Assert.True(lines.Count > 0, $"{name}: write at least one note under the header.");
        Assert.All(lines, l => Assert.True(l.Length <= 200, $"{name}: keep each note to 200 characters or fewer: \"{l}\""));
    }

    private static string NotesDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Wishlist.sln"))) dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Repository root not found."), "release-notes");
    }

    [GeneratedRegex(@"\A---\n\s*type:\s*(feature|fix|major)\s*\n---\n(?<body>[\s\S]*)\z")]
    private static partial Regex NoteFile();
}
