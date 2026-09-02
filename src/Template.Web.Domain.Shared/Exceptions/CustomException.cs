namespace Template.Web.Domain.Shared.Exceptions
{
    public abstract class CustomException : Exception
    {
        public CustomException(string msg)
            : base(msg) { }

        public abstract string ExceptionCode { get; protected set; }
    }
}
