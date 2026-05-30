using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.UseCases.Auth;
using Wishlist.Application.UseCases.GiftClaims;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Application.UseCases.Wishlists;

namespace Wishlist.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<LoginUserUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUserUseCase>();

        services.AddScoped<GetUserGroupsUseCase>();
        services.AddScoped<CreateGroupUseCase>();
        services.AddScoped<GenerateGroupInvitationUseCase>();
        services.AddScoped<JoinGroupViaInvitationUseCase>();
        services.AddScoped<ListGroupMembersUseCase>();
        services.AddScoped<LeaveGroupUseCase>();

        services.AddScoped<AddWishlistItemUseCase>();
        services.AddScoped<EditWishlistItemUseCase>();
        services.AddScoped<DeleteWishlistItemUseCase>();
        services.AddScoped<GetOwnWishlistUseCase>();
        services.AddScoped<GetMemberWishlistUseCase>();
        services.AddScoped<ListGroupWishlistsUseCase>();

        services.AddScoped<ReserveGiftUseCase>();
        services.AddScoped<MarkGiftAsBoughtUseCase>();
        services.AddScoped<ReleaseGiftClaimUseCase>();

        return services;
    }
}
