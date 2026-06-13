namespace CardWallet.Application.Exceptions;

public class BadRequestException : AppException
{
    public Dictionary<string, string[]>? Errors { get; }

    public BadRequestException(string message)
        : base(message, 400)
    {
    }

    public BadRequestException(string message, Dictionary<string, string[]> errors)
        : base(message, 400)
    {
        Errors = errors;
    }
}
