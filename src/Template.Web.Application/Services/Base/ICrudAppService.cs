using Template.Web.Application.Dtos.Base;
using Template.Web.Core;
using Template.Web.Domain.Entities.Base;

namespace Template.Web.Application.Services.Base;

public interface ICrudAppService<TEntity, TKey, TReadDto> : IBaseService
    where TEntity : AggregateRoot
    where TReadDto : IReadDto
{
    Task<TReadDto?> GetAsync(TKey key);

    Task<int> DeleteAsync(TKey key);

    Task<TReadDto?> UpdateStateAsync(TKey key, bool state);
}

public interface ICrudAppService<TEntity, TKey, TReadDto, TQueryDto>
    : ICrudAppService<TEntity, TKey, TReadDto>
    where TEntity : AggregateRoot
    where TReadDto : IReadDto
    where TQueryDto : QueryDto
{
    Task<IEnumerable<TReadDto>> GetListAsync(TQueryDto? queryDto = null);

    Task<PaginatedList<TReadDto>> GetPaginatedListAsync(TQueryDto queryDto);
}

public interface ICrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto>
    : ICrudAppService<TEntity, TKey, TReadDto, TQueryDto>
    where TEntity : AggregateRoot
    where TReadDto : IReadDto
    where TQueryDto : QueryDto
    where TCreateDto : CreateDto
{
    Task<TReadDto?> CreateAsync(TCreateDto dto);
}

public interface ICrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto, TUpdateDto>
    : ICrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto>
    where TEntity : AggregateRoot
    where TReadDto : ReadDto
    where TQueryDto : QueryDto
    where TCreateDto : CreateDto
    where TUpdateDto : UpdateDto
{
    Task<TReadDto?> UpdateAsync(TKey key, TUpdateDto dto);
}
