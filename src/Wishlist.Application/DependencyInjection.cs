using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.UseCases.Auth;
using Wishlist.Application.UseCases.Buddies;
using Wishlist.Application.UseCases.GiftClaims;
using Wishlist.Application.Messages;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Application.UseCases.LegacyImport;
using Wishlist.Application.UseCases.Wishlists;

namespace Wishlist.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegistrationGate>();
        services.AddScoped<AuthSessionFactory>();
        services.AddScoped<AccountEmails>();
        services.AddScoped<PasskeySignInUseCase>();
        services.AddScoped<PasskeySignUpUseCase>();
        services.AddScoped<PasskeyRecoveryUseCase>();
        services.AddScoped<AccountSecurityUseCase>();
        services.AddScoped<EmailConfirmationUseCase>();
        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<GetRegistrationStatusUseCase>();
        services.AddScoped<LoginUserUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUserUseCase>();
        services.AddScoped<RequestPasswordResetUseCase>();
        services.AddScoped<ResetPasswordUseCase>();

        services.AddScoped<GetUserGroupsUseCase>();
        services.AddScoped<CreateGroupUseCase>();
        services.AddScoped<GenerateGroupInvitationUseCase>();
        services.AddScoped<InvitationResolver>();
        services.AddScoped<CheckInvitationUseCase>();
        services.AddScoped<JoinGroupViaInvitationUseCase>();
        services.AddScoped<ListGroupMembersUseCase>();
        services.AddScoped<LeaveGroupUseCase>();

        services.AddScoped<AddWishlistItemUseCase>();
        services.AddScoped<EditWishlistItemUseCase>();
        services.AddScoped<DeleteWishlistItemUseCase>();
        services.AddScoped<GetOwnWishlistUseCase>();
        services.AddScoped<GetMemberWishlistUseCase>();
        services.AddScoped<ListGroupWishlistsUseCase>();
        services.AddScoped<WishlistAccess>();
        services.AddScoped<ManageBuddiesUseCase>();
        services.AddScoped<BuddyEmails>();

        services.AddScoped<ReserveGiftUseCase>();
        services.AddScoped<MarkGiftAsBoughtUseCase>();
        services.AddScoped<ReleaseGiftClaimUseCase>();
        services.AddScoped<KeepReservationUseCase>();
        services.AddScoped<ReservationEmails>();
        services.AddScoped<ProcessReservationsUseCase>();

        services.AddScoped<LoadLegacyDataUseCase>();
        services.AddScoped<GetLegacyImportStatusUseCase>();
        services.AddScoped<PreviewLegacyImportUseCase>();
        services.AddScoped<ImportLegacyItemsUseCase>();

        // Messages, shown in this order. Add new ones at the end.
        services.AddScoped<IMessageDefinition, LegacyImportMessage>();
        services.AddScoped<IMessageDefinition, WhatsNewNovember2026Message>();
        services.AddScoped<IMessageDefinition, WhatsNewBuddiesMessage>();
        services.AddScoped<GetMyMessagesUseCase>();
        services.AddScoped<DismissMessageUseCase>();

        return services;
    }
}
