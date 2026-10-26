namespace Wishlist.Application.Interfaces;

/// <summary>Reads the first sheet of a spreadsheet file into rows keyed by the header row.</summary>
public interface ISpreadsheetReader
{
    IReadOnlyList<IReadOnlyDictionary<string, string>> ReadRows(string path);
}
