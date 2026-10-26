namespace Wishlist.Application.DTOs;

/// <param name="Available">Whether old data has been loaded at all.</param>
public record LegacyImportStatusDto(bool Available, bool Imported, string? UserName, DateTime? ImportedAt, int? ItemCount);

public record LegacyPreviewRequest(string UserName);

/// <param name="Suggested">Changed on or after 1 January 2024, so ticked by default.</param>
public record LegacyItemPreviewDto(int LegacyId, string Title, string? Description, IReadOnlyList<string> Urls, DateTime? ChangedAt, bool Suggested);

public record LegacyPreviewDto(string UserName, IReadOnlyList<LegacyItemPreviewDto> Items);

public record LegacyImportRequest(string UserName, IReadOnlyList<int>? ItemIds);
