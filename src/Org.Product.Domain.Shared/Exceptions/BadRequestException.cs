namespace Org.Product.Domain.Shared.Exceptions;

public class BadRequestException : CustomException
{
    public BadRequestException(string exceptionCode, params object?[] param)
        : base(exceptionCode)
    {
        ExceptionCode = string.Format(exceptionCode, param);
    }

    public override string ExceptionCode { get; protected set; }
}
