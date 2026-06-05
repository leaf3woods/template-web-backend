using AutoMapper;
using Template.Web.Application.Dtos;
using Template.Web.Core;
using Template.Web.Domain.Entities.Account;
using Template.Web.Domain.Entities.Authority;

namespace Template.Web.Application.Utilities.MapperProfiles.DtoProfiles
{
    public class RoleDtoProfile : Profile
    {
        public RoleDtoProfile()
        {
            CreateMap(typeof(PaginatedList<>), typeof(PaginatedList<>))
                .ConvertUsing(typeof(PaginatedListConverter<,>));
            CreateMap<RoleCreateDto, Role>()
                .ForMember(dest => dest.Permissions, opt => opt.Ignore());
            CreateMap<Role, RoleReadDto>();
            CreateMap<Permission, RoleScopeReadDto>();
        }
    }
}
