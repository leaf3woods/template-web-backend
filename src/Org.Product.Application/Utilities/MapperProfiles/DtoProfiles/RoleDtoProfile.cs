using AutoMapper;
using Org.Product.Application.Dtos;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Shared;

namespace Org.Product.Application.Utilities.MapperProfiles.DtoProfiles
{
    public class RoleDtoProfile : Profile
    {
        public RoleDtoProfile()
        {
            CreateMap(typeof(PaginatedList<>), typeof(PaginatedList<>))
                .ConvertUsing(typeof(PaginatedListConverter<,>));
            CreateMap<RoleCreateDto, Role>()
                .ForMember(dest => dest.Permissions, opt => opt.Ignore());
        }
    }
}
