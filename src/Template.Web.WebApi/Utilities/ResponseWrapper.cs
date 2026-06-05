using System.Data.SqlTypes;
using Template.Web.Core;

namespace Template.Web.WebApi.Utilities
{
    public class ResponseWrapper<TRead>
    {
        public string? Info { get; set; }
        public TRead? Data { get; set; }
        public int Status { get; set; } = StatusCodes.Status200OK;

        public static ResponseWrapper<TRead> Create(
            TRead read,
            string? info = null,
            int status = StatusCodes.Status200OK
        )
        {
            return new ResponseWrapper<TRead>()
            {
                Info = info,
                Data = read,
                Status = status,
            };
        }
    }

    public static class ReadDtoWrapperExtension
    {
        public static ResponseWrapper<TRead?> Wrap<TRead>(
            this TRead? read,
            string? info = null,
            int status = StatusCodes.Status200OK
        ) =>
            new()
            {
                Info = info,
                Data = read,
                Status = status,
            };

        public static ResponseWrapper<TNull?> WrapNull<TNull>(
            string? info = null,
            int status = StatusCodes.Status200OK
        )
            where TNull : INullable =>
            new()
            {
                Info = info,
                Data = default,
                Status = status,
            };

        public static ResponseWrapper<IEnumerable<TRead>> Wrap<TRead>(
            this IEnumerable<TRead> reads,
            string? info = null,
            int status = StatusCodes.Status200OK
        ) =>
            new()
            {
                Info = info,
                Data = reads,
                Status = status,
            };

        public static ResponseWrapper<PaginatedList<TRead>> Wrap<TRead>(
            this PaginatedList<TRead> reads,
            string? info = null,
            int status = StatusCodes.Status200OK
        ) =>
            new()
            {
                Info = info,
                Data = reads,
                Status = status,
            };
    }
}
