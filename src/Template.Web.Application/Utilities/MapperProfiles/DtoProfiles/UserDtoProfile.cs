using AutoMapper;
using Template.Web.Application.Dtos;
using Template.Web.Domain.Entities.Account;
using Template.Web.Domain.Utilities;
using Template.Web.Domain.ValueObjects.UserValue;

namespace Template.Web.Application.Utilities.MapperProfiles.DtoProfiles
{
    public class UserDtoProfile : Profile
    {
        public UserDtoProfile()
        {
            CreateMap<User, UserReadDto>();
            CreateMap<UserRegisterDto, User>()
                .AfterMap(
                    (src, dest) =>
                    {
                        var bytes = Convert.FromBase64String(src.Password);
                        dest.Passphrase = Convert.ToBase64String(
                            CryptoUtil.Salt(bytes, out var salt)
                        );
                        dest.Salt = Convert.ToBase64String(salt);
                        dest.Roles = [Role.MemberRole];
                    }
                );
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
