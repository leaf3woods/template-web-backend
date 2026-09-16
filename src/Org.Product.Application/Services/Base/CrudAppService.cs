using AutoMapper;
using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Base;
using Org.Product.Domain.Repositories;
using Org.Product.Domain.Shared;

namespace Org.Product.Application.Services.Base;

public abstract class CrudAppService<TEntity, TKey, TReadDto>
    : BaseService,
        ICrudAppService<TEntity, TKey, TReadDto>
    where TEntity : IAggregateRoot
    where TReadDto : IReadDto
{
    protected CrudAppService(
        IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper
    )
        : base(mapper)
    {
        Repository = repository;
        UnitOfWork = unitOfWork;
    }

    protected IRepository<TEntity> Repository { get; }

    protected IUnitOfWork UnitOfWork { get; }

    protected IQueryable<TEntity> Queryable => Repository.Query();

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual async Task<int> DeleteAsync(TKey key)
    {
        var entity = await Repository.FindAsync(key);
        if (entity is null)
        {
            return 0;
        }

        Repository.Remove(entity);
        return await UnitOfWork.SaveChangesAsync();
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
        var entity = await Repository.FindAsync(key);
        return Mapper.Map<TReadDto>(entity);
    }
}

public abstract class CrudAppService<TEntity, TKey, TReadDto, TQueryDto>
    : CrudAppService<TEntity, TKey, TReadDto>,
        ICrudAppService<TEntity, TKey, TReadDto, TQueryDto>
    where TEntity : IAggregateRoot
    where TReadDto : IReadDto
    where TQueryDto : QueryDto
{
    protected CrudAppService(
        IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper
    )
        : base(repository, unitOfWork, mapper) { }

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

public abstract class CrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto>
    : CrudAppService<TEntity, TKey, TReadDto, TQueryDto>,
        ICrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto>
    where TEntity : IAggregateRoot
    where TReadDto : IReadDto
    where TQueryDto : QueryDto
    where TCreateDto : CreateDto
{
    protected CrudAppService(
        IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper
    )
        : base(repository, unitOfWork, mapper) { }

    /// <summary>
    /// 创建实体
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public virtual async Task<TReadDto?> CreateAsync(TCreateDto dto)
    {
        var entity = Mapper.Map<TEntity>(dto);
        await Repository.AddAsync(entity);
        await UnitOfWork.SaveChangesAsync();
        return Mapper.Map<TReadDto>(entity);
    }
}

public abstract class CrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto, TUpdateDto>
    : CrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto>,
        ICrudAppService<TEntity, TKey, TReadDto, TQueryDto, TCreateDto, TUpdateDto>
    where TEntity : IAggregateRoot
    where TReadDto : IReadDto
    where TQueryDto : QueryDto
    where TCreateDto : CreateDto
    where TUpdateDto : UpdateDto
{
    protected CrudAppService(
        IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper
    )
        : base(repository, unitOfWork, mapper) { }

    /// <summary>
    /// 修改实体
    /// </summary>
    /// <param name="key"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    public virtual async Task<TReadDto?> UpdateAsync(TKey key, TUpdateDto dto)
    {
        var entity = await Repository.FindAsync(key);
        if (entity is null)
        {
            return default;
        }

        Mapper.Map(dto, entity);
        Repository.Update(entity);
        await UnitOfWork.SaveChangesAsync();
        return Mapper.Map<TReadDto>(entity);
    }
}
