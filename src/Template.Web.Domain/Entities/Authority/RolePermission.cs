using Template.Web.Domain.Entities.Account;
using Template.Web.Domain.Entities.Base;

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
