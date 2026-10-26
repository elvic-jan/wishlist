using System.IO.Compression;
using System.Xml.Linq;
using Wishlist.Application.Interfaces;

namespace Wishlist.Infrastructure.Services;

/// <summary>
/// Reads the first worksheet of an .xlsx file (a zip of XML parts) without a
/// spreadsheet library: shared strings, inline strings and plain values. Enough
/// for flat table exports; formulas, styles and dates are returned as stored.
/// </summary>
public sealed class XlsxReader : ISpreadsheetReader
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PkgRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    public IReadOnlyList<IReadOnlyDictionary<string, string>> ReadRows(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        var shared = ReadSharedStrings(zip);
        var sheet = Load(zip, FirstSheetPath(zip)) ?? throw new InvalidDataException($"{path} has no worksheet.");

        var rows = sheet.Descendants(Ns + "row")
            .Select(row => row.Elements(Ns + "c").ToDictionary(c => ColumnIndex((string)c.Attribute("r")!), c => CellValue(c, shared)))
            .ToList();
        if (rows.Count == 0) return [];

        var header = rows[0];
        return rows.Skip(1)
            .Select(r => (IReadOnlyDictionary<string, string>)header.ToDictionary(
                h => h.Value.Trim(),
                h => r.TryGetValue(h.Key, out var v) ? v : string.Empty))
            .ToList();
    }

    private static string FirstSheetPath(ZipArchive zip)
    {
        var workbook = Load(zip, "xl/workbook.xml");
        var rels = Load(zip, "xl/_rels/workbook.xml.rels");
        var relId = (string?)workbook?.Descendants(Ns + "sheet").FirstOrDefault()?.Attribute(RelNs + "id");
        var target = (string?)rels?.Descendants(PkgRelNs + "Relationship")
            .FirstOrDefault(r => (string?)r.Attribute("Id") == relId)?.Attribute("Target");
        if (target is null) return "xl/worksheets/sheet1.xml";
        return target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
    }

    private static List<string> ReadSharedStrings(ZipArchive zip) =>
        Load(zip, "xl/sharedStrings.xml")?.Root?.Elements(Ns + "si")
            .Select(si => string.Concat(si.Descendants(Ns + "t").Select(t => t.Value)))
            .ToList() ?? [];

    private static string CellValue(XElement cell, List<string> shared)
    {
        var type = (string?)cell.Attribute("t");
        if (type == "inlineStr")
            return string.Concat(cell.Descendants(Ns + "t").Select(t => t.Value));

        var value = cell.Element(Ns + "v")?.Value ?? string.Empty;
        return type == "s" && int.TryParse(value, out var i) && i < shared.Count ? shared[i] : value;
    }

    private static int ColumnIndex(string cellRef)
    {
        var n = 0;
        foreach (var ch in cellRef.TakeWhile(char.IsLetter))
            n = n * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        return n - 1;
    }

    private static XDocument? Load(ZipArchive zip, string entry)
    {
        var e = zip.GetEntry(entry);
        if (e is null) return null;
        using var stream = e.Open();
        return XDocument.Load(stream);
    }
}
