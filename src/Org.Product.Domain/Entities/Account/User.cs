using Org.Product.Domain.Entities.Base;
using Org.Product.Domain.Entities.Base.Audited;
using Org.Product.Domain.ValueObjects.UserValue;

namespace Org.Product.Domain.Entities.Account;

public class User : AggregateRoot<Guid>, ISoftDelete, IAudited, IHasSortOrder, IEnableable
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

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; }

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
        Id = new Guid("a8ba3a6d-c7b1-4d90-b0c9-66f1cbd66101"),
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
        Id = new Guid("a8ba3a6d-c7b1-4d90-b0c9-66f1cbd66102"),
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
        Id = new Guid("a8ba3a6d-c7b1-4d90-b0c9-66f1cbd66103"),
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
