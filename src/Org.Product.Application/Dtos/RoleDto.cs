using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Entities.Authority;

namespace Org.Product.Application.Dtos
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
