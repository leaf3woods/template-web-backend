using AutoMapper;
using Org.Product.Application.Dtos.Cache;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Utilities.MapperProfiles.CacheItemProfiles;

public sealed class RolePermissionsCacheProfile : Profile
{
    public RolePermissionsCacheProfile()
    {
        CreateMap<Role, RolePermissionsCacheItem>()
            .ForMember(
                item => item.PermissionCodes,
                options =>
                    options.MapFrom(role =>
                        role.Permissions!.Where(permission => permission.PermissionCode != null)
                            .Select(permission => permission.PermissionCode!)
                    )
            );
    }
}
