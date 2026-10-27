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

    /// <param name="code">Dil bağımsız, istemcinin tepki verebileceği kod (bkz. <see cref="ErrorCodes"/>).</param>
    public ForbiddenAccessException(string message, string code)
        : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
