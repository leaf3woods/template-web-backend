using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Entities.Base;

namespace Org.Product.Domain.Entities.Authority
{
    public class RolePermission : AggregateRoot<long>
    {
        public Guid PermissionId { get; set; }

        public Permission Permission { get; set; } = null!;

        public Guid RoleId { get; set; }

        public Role Role { get; set; } = null!;
    }
}
