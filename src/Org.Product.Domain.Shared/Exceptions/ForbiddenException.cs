namespace Org.Product.Domain.Shared.Exceptions
{
    public class ForbiddenException : CustomException
    {
        public ForbiddenException(string exceptionCode, params object?[] param)
            : base(exceptionCode)
        {
            ExceptionCode = string.Format(exceptionCode, param);
        }

        public override string ExceptionCode { get; protected set; }
    }
}
