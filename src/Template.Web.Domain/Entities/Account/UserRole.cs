using Template.Web.Domain.Entities.Base;

namespace Template.Web.Domain.Entities.Account
{
    public class UserRole : IncrementEntity
    {
        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public Guid RoleId { get; set; }

        public Role Role { get; set; } = null!;
    }
}
