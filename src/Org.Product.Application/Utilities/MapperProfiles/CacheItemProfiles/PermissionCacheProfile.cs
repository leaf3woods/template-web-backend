using AutoMapper;
using Org.Product.Application.Dtos.Cache;
using Org.Product.Domain.Entities.Authority;

namespace Org.Product.Application.Utilities.MapperProfiles.CacheItemProfiles;

public class PermissionCacheProfile : Profile
{
    public PermissionCacheProfile()
    {
        CreateMap<RolePermission, PermissionCacheItem>()
            .ForMember(
                destination => destination.Name,
                options => options.MapFrom(source => source.Permission.Name)
            )
            .ForMember(
                destination => destination.Code,
                options => options.MapFrom(source => source.Permission.Code)
            )
            .ForMember(
                destination => destination.SortOrder,
                options => options.MapFrom(source => source.Permission.SortOrder)
            )
            .ForMember(
                destination => destination.IsEnabled,
                options => options.MapFrom(source => source.Permission.IsEnabled)
            );
    }
}
