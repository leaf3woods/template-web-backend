namespace Org.Product.Domain.Entities.Base;

public interface ISoftDelete
{
    public bool SoftDeleted { get; set; }

    public DateTime? DeleteTime { get; set; }
}
