using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Org.Product.Domain.Entities.Base;
using Org.Product.Domain.Entities.Base.Audited;

namespace Org.Product.Infrastructure.Utilities;

public static class EntityEntryExtension
{
    public static EntityEntry ApplySoftDelete(this EntityEntry entry)
    {
        if (entry.State == EntityState.Deleted && entry.Entity is ISoftDelete delete)
        {
            entry.State = EntityState.Unchanged;
            delete.SoftDeleted = true;
            delete.DeleteTime = DateTime.UtcNow;
        }
        return entry;
    }

    public static EntityEntry ApplyAuditedData(this EntityEntry entry, Guid? userId)
    {
        if (entry.State == EntityState.Added && entry.Entity is ICreationAudited creationAudited)
        {
            creationAudited.CreationTime = DateTime.UtcNow;
            creationAudited.CreatorId = userId;
        }
        if (
            entry.State == EntityState.Modified
            && entry.Entity is ILastModificationAudited lastModificationAudited
        )
        {
            lastModificationAudited.LastModificationTime = DateTime.UtcNow;
            lastModificationAudited.LastModifierId = userId;
        }

        return entry;
    }

    public static EntityEntry ApplyPendingInvalidations(
        this EntityEntry entry,
        Dictionary<string, HashSet<object>> invalidations
    )
    {
        if (entry.State is EntityState.Deleted or EntityState.Modified or EntityState.Added)
        {
            var primaryKey = entry.Metadata.FindPrimaryKey();
            var entityName = entry.Metadata.ClrType.Name;
            if (primaryKey is not null)
            {
                var key = entry.Property(primaryKey.Properties[0].Name).CurrentValue;
                if (key is not null)
                {
                    if (!invalidations.TryGetValue(entityName, out var list))
                    {
                        invalidations[entityName] = list = [];
                    }

                    list.Add(key);
                }
            }
        }
        return entry;
    }
}
