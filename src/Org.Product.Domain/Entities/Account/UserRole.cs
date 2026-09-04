using Org.Product.Domain.Entities.Base;

namespace Org.Product.Domain.Entities.Account
{
    public class UserRole : AggregateRoot<long>
    {
        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public Guid RoleId { get; set; }

        public Role Role { get; set; } = null!;
    }
}
