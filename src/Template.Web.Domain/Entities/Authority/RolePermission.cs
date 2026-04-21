using Template.Web.Domain.Entities.Base;
using Template.Web.Domain.Entities.Account;

namespace Template.Web.Domain.Entities.Authority
{
    public class RolePermission : IncrementEntity
    {
        public Guid PermissionId { get; set; }

        public Permission Permission { get; set; } = null!;

        public Guid RoleId { get; set; }

        public Role Role { get; set; } = null!;
    }
}
