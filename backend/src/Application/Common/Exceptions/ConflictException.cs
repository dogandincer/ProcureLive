namespace ProcureLive.Application.Common.Exceptions;

/// <summary>Business-rule conflict, e.g. duplicate tax number, unique constraint violation.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
