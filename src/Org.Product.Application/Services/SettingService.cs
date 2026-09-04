using Org.Product.Application.Services.Base;

namespace Org.Product.Application.Services
{
    public class SettingService : BaseService, ISettingService
    {
        public SettingService(AutoMapper.IMapper mapper)
            : base(mapper) { }
    }
}
