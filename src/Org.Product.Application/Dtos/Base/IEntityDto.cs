namespace Org.Product.Application.Dtos.Base;

public interface IEntityDto { }

/// <summary>
///     Declares the aggregate or entity represented by a DTO.
/// </summary>
public interface IEntityDto<TEntity> : IEntityDto { }
