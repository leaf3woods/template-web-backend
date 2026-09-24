using AutoMapper;
using Org.Product.Application.Dtos.Cache;
using Org.Product.Domain.Entities.Authority;

namespace Org.Product.Application.Utilities.MapperProfiles.CacheItemProfiles;

public class PermissionCacheProfile : Profile
{
    public PermissionCacheProfile()
    {
        CreateMap<Permission, PermissionCacheItem>();
    }
}
