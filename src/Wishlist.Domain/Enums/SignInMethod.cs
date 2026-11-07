namespace Wishlist.Domain.Enums;

/// <summary>How a new session was started.</summary>
public enum SignInMethod
{
    Password,
    Passkey,
    Recovery,
    SignUp
}
