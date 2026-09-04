using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Org.Product.Domain.Shared;
using Org.Product.Domain.Shared.Exceptions;
using Org.Product.WebApi.Exceptions;

namespace Org.Product.WebApi.Utilities
{
    public static class ExceptionLocalizerExtension
    {
        public static async Task LocalizeException(
            HttpContext context,
            IStringLocalizer stringLocalizer,
            bool includeExceptionDetails
        )
        {
            context.Response.ContentType = "application/json";
            var exception = context.Features.Get<IExceptionHandlerFeature>();
            if (exception != null)
            {
                context.Response.StatusCode = exception.Error switch
                {
                    NotFoundException => StatusCodes.Status404NotFound,
                    NotAcceptableException => StatusCodes.Status406NotAcceptable,
                    _ => StatusCodes.Status500InternalServerError,
                };

                var errDto = exception.Error.Localize(stringLocalizer, includeExceptionDetails);
                await context.Response.WriteAsJsonAsync(
                    errDto,
                    SharedOptions.CustomJsonSerializerOptions
                );
            }
        }
    }
}
