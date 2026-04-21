using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Template.Web.Application.Dtos.Base;
using Template.Web.Core;
using Template.Web.Domain.Entities.Base;
using Template.Web.Infrastructure.DbContexts;

namespace Template.Web.Application.Services.Base;

public abstract class CrudAppService<TEntity, TKey, TReadDto> : BaseService,
    ICrudAppService<TEntity, TKey, TReadDto>
    where TEntity : AggregateRoot
    where TReadDto : ReadDto
{
    /// <summary>
    ///     aoc 属性注入
    /// </summary>
    public ApiDbContext DbContext { get; init; } = null!;
    public IHttpContextAccessor? HttpContextAccessor { get; init; }
    public ILogger<TEntity> Logger { get; init; } = null!;

    protected IQueryable<TEntity> Queryable { get => DbContext.Set<TEntity>().AsQueryable(); }

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual async Task<int> DeleteAsync(TKey key)
    {
        var entity = await DbContext.Set<TEntity>().FindAsync(key);
        if (entity != null)
        {
            var delete = DbContext.Set<TEntity>().Remove(entity);
            var count = await DbContext.SaveChangesAsync();
            return count;
        }
        return 0;
    }

    public Task<TReadDto?> UpdateStateAsync(TKey key, bool state)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<TReadDto?> GetAsync(TKey key)
    {
        var entity = await DbContext.Set<TEntity>().FindAsync(key);
        return Mapper.Map<TReadDto>(entity);
    }
}

public abstract class CrudAppService<TEntity, TKey, TReadDto, TQueryDto> :
    CrudAppService<TEntity, TKey, TReadDto>,
    ICrudAppService<TEntity, TKey, TReadDto, TQueryDto>
    where TEntity : AggregateRoot
    where TReadDto : ReadDto
    where TQueryDto : QueryDto
{
    /// <summary>
    /// 查询实体列表
    /// </summary>
    /// <param name="queryDto"></param>
    /// <returns></returns>
    public abstract Task<IEnumerable<TReadDto>> GetListAsync(TQueryDto? queryDto = null);

    /// <summary>
    /// 分页查询实体列表
    /// </summary>
    /// <param name="queryDto"></param>
    /// <returns></returns>
    public abstract Task<PaginatedList<TReadDto>> GetPaginatedListAsync(TQueryDto queryDto);
}

public abstract class CrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto> :
    CrudAppService<TEntity, TKey, TReadDto, TQueryDto>,
    ICrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto>
    where TEntity : AggregateRoot
    where TReadDto : ReadDto
    where TQueryDto : QueryDto
    where TCreateDto : CreateDto
{
    /// <summary>
    /// 创建实体
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<TReadDto?> CreateAsync(TCreateDto dto)
    {
        var entity = Mapper.Map<TEntity>(dto);
        await DbContext.Set<TEntity>().AddAsync(entity);
        await DbContext.SaveChangesAsync();
        return Mapper.Map<TReadDto>(entity);
    }
}

public abstract class CrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto, TUpdateDto> :
    CrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto>,
    ICrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto, TUpdateDto>
    where TEntity : AggregateRoot
    where TReadDto : ReadDto
    where TQueryDto : QueryDto
    where TCreateDto : CreateDto
    where TUpdateDto : UpdateDto
{
    /// <summary>
    /// 修改实体
    /// </summary>
    /// <param name="key"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    public virtual async Task<TReadDto?> UpdateAsync(TKey key, TUpdateDto dto)
    {
        var entity = await DbContext.Set<TEntity>().FindAsync(key);
        if (entity == null)
        {
            return default;
        }
        Mapper.Map(dto, entity);
        DbContext.Set<TEntity>().Update(entity);
        await DbContext.SaveChangesAsync();
        return Mapper.Map<TReadDto>(entity);
    }
}