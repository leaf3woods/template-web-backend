using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Authority;

namespace Org.Product.Application.Dtos;

public class MenuReadDto : ReadDto<Permission>
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    public int Type { get; set; }

    public int SortOrder { get; set; }

    public int Level { get; set; }

    public string Path { get; set; } = null!;

    public string IconUrl { get; set; } = null!;

    public string Route { get; set; } = null!;

    public bool Visible { get; set; }

    public bool Favorite { get; set; }

    public IEnumerable<MenuReadDto>? Children { get; set; }
}

public class MenuCreateDto : CreateDto<Permission>
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    public int Type { get; set; }

    public int SortOrder { get; set; }

    public string Path { get; set; } = null!;

    public string IconUrl { get; set; } = null!;

    public string Route { get; set; } = null!;

    public bool Visible { get; set; }

    public bool Favorite { get; set; }
}

public class MenuUpdateDto : UpdateDto<Permission>
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    public int Type { get; set; }

    public int SortOrder { get; set; }

    public string Path { get; set; } = null!;

    public string IconUrl { get; set; } = null!;

    public string Route { get; set; } = null!;

    public bool Visible { get; set; }

    public bool Favorite { get; set; }

    public bool IsEnabled { get; set; }
}

public class MenuQueryDto : PaginatedQueryDto
{
    public string? Name { get; set; }

    public string? Code { get; set; }

    public Guid? ParentId { get; set; }

    public int? Type { get; set; }

    public bool? Visible { get; set; }

    public bool? IsEnabled { get; set; }
}
