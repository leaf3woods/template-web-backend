using Microsoft.Extensions.Localization;
using Template.Web.Application.Dtos;
using Template.Web.Domain.Shared.Exceptions;

namespace Template.Web.WebApi.Exceptions
{
    public static class ExceptionExtension
    {
        public static ExceptionReadDto Localize(
            this Exception exception,
            IStringLocalizer stringLocalizer,
            bool includeExceptionDetails
        )
        {
            var result = exception switch
            {
                NotFoundException or NotAcceptableException or ForbiddenException =>
                    new ExceptionReadDto()
                    {
                        Info = stringLocalizer[(exception as CustomException)!.ExceptionCode],
                    },
                _ when includeExceptionDetails => new ExceptionReadDto
                {
                    Info = exception.Message.Split("\r\n", StringSplitOptions.TrimEntries)[0],
                    StackTrace = exception.StackTrace?.Split(
                        "\r\n",
                        StringSplitOptions.TrimEntries
                    )[0],
                    Inner = exception.InnerException?.Message.Split(
                        "\r\n",
                        StringSplitOptions.TrimEntries
                    )[0],
                },
                _ => new ExceptionReadDto
                {
                    Info = exception.Message.Split("\r\n", StringSplitOptions.TrimEntries)[0],
                    StackTrace = null,
                    Inner = null,
                },
            };
            return result;
        }
    }
}
