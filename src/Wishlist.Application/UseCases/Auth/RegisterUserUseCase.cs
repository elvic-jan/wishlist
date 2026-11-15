using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.Localization;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class RegisterUserUseCase(
    IUserRepository users,
    RegistrationGate gate,
    NewAccountInvitation newAccountInvitation,
    IUnitOfWork unitOfWork,
    AuthSessionFactory sessions,
    AccountEmails emails,
    ILanguageContext language)
{
    public async Task<(AuthResponse Response, string RefreshToken)> ExecuteAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (request.Password is null || request.Password.Length < 8)
            throw new DomainException("Invalid registration data.");
        InputLimits.Check(request.Password, InputLimits.Password, "Password");

        var invitation = await gate.CheckAsync(request.Email, request.DisplayName, request.InviteCode, ct);

        // Account and membership commit together: no account may exist without
        // the group it was invited into.
        User user = null!;
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            user = await users.CreateAsync(request.Email, request.DisplayName, request.Password, language.Language, innerCt);
            await newAccountInvitation.RedeemAsync(invitation, user.Id, innerCt);
        }, ct);

        await emails.SendEmailConfirmationAsync(user, ct);
        return await sessions.CreateAsync(user, SignInMethod.SignUp, invitation.GroupId, ct);
    }
}
