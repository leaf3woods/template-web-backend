namespace Org.Product.Application.Dtos.Base;

public abstract class ReadDto : IReadDto
{
    public Guid Id { get; set; }
}

public abstract class ReadDto<TEntity> : ReadDto, IEntityDto<TEntity> { }

public abstract class ReadDto<TEntity, TKey> : IReadDto, IEntityDto<TEntity>
    where TKey : notnull
{
    public TKey Id { get; set; } = default!;
}
