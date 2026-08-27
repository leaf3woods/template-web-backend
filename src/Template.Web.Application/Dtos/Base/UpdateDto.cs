namespace Template.Web.Application.Dtos.Base
{
    public abstract class UpdateDto { }

    public abstract class UpdateDto<TEntity> : UpdateDto, IEntityDto<TEntity> { }
}
