using Template.Web.Application.Services.Base;

namespace Template.Web.Application.Services
{
    public class SettingService : BaseService, ISettingService
    {
        public SettingService(AutoMapper.IMapper mapper)
            : base(mapper) { }
    }
}
