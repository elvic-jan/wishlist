namespace Wishlist.Application.DTOs;

public record AdminTotals(int Users, int Circles, int Wishes, int ActiveReservations);

public record MonthlySignUpsDto(int Year, int Month, int Count);

/// <param name="Days">Length of the period: 7 or 30.</param>
/// <param name="Current">Users active in the last <paramref name="Days"/> days, today included.</param>
/// <param name="Previous">Users active in the <paramref name="Days"/> days before that; null while
/// tracking doesn't cover that whole period yet.</param>
public record ActiveUsersDto(int Days, int Current, int? Previous);

public record AdminOverviewDto(
    AdminTotals Totals,
    int SignUpsThisMonth,
    int SignUpsLastMonth,
    IReadOnlyList<MonthlySignUpsDto> SignUpsByMonth,
    ActiveUsersDto Active7,
    ActiveUsersDto Active30,
    DateOnly? TrackingSince);

public record AdminUserDto(
    Guid Id,
    string DisplayName,
    string Email,
    DateTime CreatedAt,
    bool EmailConfirmed,
    bool HasPassword,
    bool HasPasskey,
    bool IsAdmin,
    int Circles,
    DateTime? LastSignInAt,
    int SignIns,
    DateOnly? LastActiveOn);
