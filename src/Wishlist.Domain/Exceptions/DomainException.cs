namespace Wishlist.Domain.Exceptions;

/// <summary>
/// An error meant for the person using the app. <see cref="Exception.Message"/>
/// is the English text; <see cref="Template"/> and <see cref="Arguments"/> let
/// the API answer in the person's language.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
        Template = message;
        Arguments = [];
    }

    /// <summary>A message with values: <c>new DomainException("Pick between 1 and {0} people.", max)</c>.</summary>
    public DomainException(string template, params object[] arguments) : base(string.Format(template, arguments))
    {
        Template = template;
        Arguments = arguments;
    }

    /// <summary>The English text with <c>{0}</c> placeholders; the key of its translations.</summary>
    public string Template { get; }

    public IReadOnlyList<object> Arguments { get; }
}

/// <summary>A word or phrase of the app (not user input) used as a value in a message, so it is translated too.</summary>
public sealed record Term(string English)
{
    public override string ToString() => English;
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
