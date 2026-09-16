using Org.Product.Application.Abstractions.Security;
using AutoMapper;
using Org.Product.Application.Dtos;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.ValueObjects.UserValue;

namespace Org.Product.Application.Utilities.MapperProfiles.DtoProfiles
{
    public class UserDtoProfile : Profile
    {
        public UserDtoProfile()
        {
            CreateMap<UserRegisterDto, User>()
                .ForMember(dest => dest.Passphrase, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.Roles, opt => opt.Ignore());
            CreateMap<UserSetting, UserSettingReadDto>();
            CreateMap<UserSetting, UserDetailReadDto>();
            CreateMap<Captcha, CaptchaReadDto>()
                .ForMember(
                    dest => dest.Image,
                    opts => opts.MapFrom(src => src.Image == null ? string.Empty : src.ToString())
                )
                .ForMember(dest => dest.Type, opts => opts.MapFrom(src => src.Type.ToString("F")))
                .ForMember(
                    dest => dest.Pixel,
                    opts => opts.MapFrom(src => new int[] { src.Pixel.Item1, src.Pixel.Item2 })
                );

            CreateMap<CaptchaAnswerDto, Captcha>()
                .ForMember(dest => dest.Image, opts => opts.Ignore())
                .ForMember(dest => dest.Pixel, opts => opts.Ignore());
        }
    }
}
