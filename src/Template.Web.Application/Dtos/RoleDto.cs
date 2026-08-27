using Template.Web.Application.Dtos.Base;
using Template.Web.Domain.Entities.Account;
using Template.Web.Domain.Entities.Authority;

namespace Template.Web.Application.Dtos
{
    public class RoleCreateDto : CreateDto<Role>, IManualDtoMapping
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? Description { get; set; }
        public IEnumerable<string> ScopeNames { get; set; } = null!;
    }

    public class RoleReadDto : ReadDto<Role>
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public IEnumerable<RoleScopeReadDto> Scopes { get; set; } = null!;
    }

    public class RoleQueryDto : PaginatedQueryDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }

        public bool? State { get; set; }
    }

    public class RoleUpdateDto : UpdateDto<Role>
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public IEnumerable<Guid>? MenuIds { get; set; } = null!;

        public IEnumerable<Guid>? UserIds { get; set; } = null!;
    }

    public class RoleScopeReadDto : ReadDto<Permission>
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class RoleScopeModifyDto : UpdateDto<Permission>
    {
        public string Name { get; set; } = null!;
    }
}
