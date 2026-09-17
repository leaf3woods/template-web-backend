namespace Org.Product.Domain.Entities.Base;

public interface ISoftDelete
{
    bool SoftDeleted { get; set; }

    DateTime? DeleteTime { get; set; }
}
