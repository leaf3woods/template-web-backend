using AutoMapper;
using Template.Web.Domain.Shared;

namespace Template.Web.Application.Utilities
{
    public class PaginatedListConverter<TEntity, TDto>
        : ITypeConverter<PaginatedList<TEntity>, PaginatedList<TDto>>
    {
        public PaginatedList<TDto> Convert(
            PaginatedList<TEntity> source,
            PaginatedList<TDto> destination,
            ResolutionContext context
        )
        {
            destination = new PaginatedList<TDto>(
                context.Mapper.Map<IEnumerable<TDto>>(source.Content),
                source.TotalItems,
                source.PageIndex,
                source.PageSize
            );
            return destination;
        }
    }
}
