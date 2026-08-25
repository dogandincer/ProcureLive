namespace ProcureLive.Application.Common.Exceptions;

public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Bu kaynağa erişim yetkiniz yok.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
}
