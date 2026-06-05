using Template.Web.Domain.Entities.Base;
using Template.Web.Domain.Entities.Base.Audited;
using Template.Web.Domain.ValueObjects.UserValue;

namespace Template.Web.Domain.Entities.Account
{
    public class User : UniversalEntity, ISoftDelete, IAudited, IOrder, IState
    {
        /// <summary>
        ///     用户名
        /// </summary>
        public string Username { get; set; } = null!;
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Nick { get; set; }

        public string Passphrase { get; set; } = null!;
        public string Salt { get; set; } = null!;

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public Gender Gender { get; set; } = Gender.Unknow;

        /// <summary>
        /// 年龄
        /// </summary>
        public DateTime? BirthDate { get; set; }

        public string? Note { get; set; }

        public DateTime RegisterTime { get; set; }
        public UserSetting? Settings { get; set; }
        public UserDetail? Detail { get; set; }

        #region navigation

        public virtual IEnumerable<Role> Roles { get; set; } = null!;

        #endregion navigation

        public int Order { get; set; }

        public bool State { get; set; }

        #region audit

        public Guid? CreatorId { get; set; }
        public int? CreatorLevel { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? LastModifierId { get; set; }
        public DateTime? LastModificationTime { get; set; }

        #endregion audit

        #region delete filter

        public bool SoftDeleted { get; set; } = false;
        public DateTime? DeleteTime { get; set; }

        #endregion delete filter

        public static readonly User DevUser = new()
        {
            Username = "developer",
            Passphrase = "Uh+8E9ft9jptdMzAVRKo0UYQtqn5epsbJUZQGbL/Xhk=",
            Salt = "5+fPPv0FShtKo3ed746TiuNojEZsxuPkhbU+YvF5DuQ=",
            Nick = "initial-developer",
            Email = "unknow",
            PhoneNumber = "unknow",
            RegisterTime = DateTime.UnixEpoch,
        };

        public static readonly User SuperUser = new()
        {
            Username = "super",
            Passphrase = "WSAcdSAvzQFUq3iXLWXLmcuPmWHIjE8ffSBTVjJVBPQ=",
            Salt = "aY68cuKZh+LNfYczaGclgtTOYy34yvl1O/H9IX3bBtU=",
            Nick = "initial-super",
            Email = "unknow",
            PhoneNumber = "unknow",
            RegisterTime = DateTime.UnixEpoch,
        };

        public static readonly User AdminUser = new()
        {
            Username = "admin",
            Passphrase = "Lc8DL5jIpDxDfsDp6gYk2HjVIEzXZ30MJc5eW6OU6ko=",
            Salt = "JO3wh7gOTUQ5cBydCoQqnazvw5dgRoVQkNpdrIAvVgI=",
            Nick = "initial-admin",
            Email = "unknow",
            PhoneNumber = "unknow",
            RegisterTime = DateTime.UnixEpoch,
        };

        public static User[] Seeds { get; } = { DevUser, SuperUser, AdminUser };
    }
}
