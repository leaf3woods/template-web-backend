namespace Org.Product.Application.Dtos.Base;

public abstract class CreateDto { }

public abstract class CreateDto<TEntity> : CreateDto, IEntityDto<TEntity> { }
