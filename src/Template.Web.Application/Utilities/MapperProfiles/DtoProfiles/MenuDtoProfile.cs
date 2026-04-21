using AutoMapper;
using Template.Web.Application.Dtos;
using Template.Web.Domain.Entities.Authority;

namespace Template.Web.Application.Utilities.MapperProfiles.DtoProfiles
{
    public class MenuDtoProfile : Profile
    {
        public MenuDtoProfile()
        {
            CreateMap<Permission, MenuReadDto>();
            CreateMap<MenuCreateDto, Permission>();
        }
    }
}
