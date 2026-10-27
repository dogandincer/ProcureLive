namespace ProcureLive.Application.Common.Exceptions;

/// <summary>Business-rule conflict, e.g. duplicate tax number, unique constraint violation.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    /// <param name="code">Dil bağımsız, istemcinin tepki verebileceği kod (bkz. <see cref="ErrorCodes"/>).</param>
    public ConflictException(string message, string code)
        : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
