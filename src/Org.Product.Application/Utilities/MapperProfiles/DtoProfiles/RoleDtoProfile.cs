using AutoMapper;
using Org.Product.Application.Dtos;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Utilities.MapperProfiles.DtoProfiles;

public class RoleDtoProfile : Profile
{
    public RoleDtoProfile()
    {
        CreateMap<RoleCreateDto, Role>()
            .ForMember(dest => dest.Permissions, opt => opt.Ignore());
    }
}
