using AutoMapper;
using Template.Web.Application.Dtos;
using Template.Web.Domain.Entities.Account;
using Template.Web.Domain.Shared;

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
        }
    }
}
