using Template.Web.Domain.Entities.Base;
using Template.Web.Domain.Entities.Base.Audited;

namespace Template.Web.Domain.Entities.Authority
{
    public class Permission : UniversalEntity, ISoftDelete, IAudited, IOrder, IState
    {
        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? Description { get; set; }

        /// <summary>
        /// 前端组件路径
        /// </summary>
        /// <summary>
        /// 调用的接口地址
        /// </summary>
        public string? Route { get; set; }

        public string? PermissionCode { get; set; }

        public bool Visible { get; set; } = true;

        public bool Favorite { get; set; } = false;

        public bool Visiable { get; set; } = true;

        public bool IsLink { get; set; }
        public string? IconUrl { get; set; }

        public PermissionType Type { get; set; }

        public int Order { get; set; }
        public bool State { get; set; }

        #region navigation

        public Guid? ParentId { get; set; }
        public Permission? Parent { get; set; }

        #endregion navigation

        #region delete filter

        public bool SoftDeleted { get; set; } = false;
        public DateTime? DeleteTime { get; set; }

        #endregion delete filter

        #region audit

        public Guid? CreatorId { get; set; }
        public int? CreatorLevel { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? LastModifierId { get; set; }
        public DateTime? LastModificationTime { get; set; }

        #endregion audit

        #region static

        public static Permission Root = new()
        {
            Id = new Guid("e9df3280-8ab1-4b45-8d6a-6c3e669317ac"),
            Name = "Root",
            Code = "root",
            Description = "root menu",
        };

        public static IEnumerable<Permission> Seeds { get; } = [Root];

        #endregion
    }
}
