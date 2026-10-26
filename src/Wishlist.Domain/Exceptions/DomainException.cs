namespace Wishlist.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

public class TooManyRequestsException : DomainException
{
    public TooManyRequestsException(string message) : base(message) { }
}

public class GoneException : DomainException
{
    public GoneException(string message) : base(message) { }
}

/// <summary>
/// An invite code that does not exist (or is not even well-formed). Distinct from
/// other not-found cases so callers can count it as a wrong guess.
/// </summary>
public class InvitationNotFoundException : NotFoundException
{
    public InvitationNotFoundException() : base("Invitation not found.") { }
}
