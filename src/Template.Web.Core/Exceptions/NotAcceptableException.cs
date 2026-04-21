namespace Template.Web.Core.Exceptions
{
    public class NotAcceptableException : CustomException
    {
        public NotAcceptableException(string exceptionCode, params object?[] param) : base(exceptionCode)
        {
            ExceptionCode = string.Format(exceptionCode, param);
        }

        public override string ExceptionCode { get; protected set; }
    }
}