using AutoMapper;

namespace Template.Web.Application.Services.Base
{
    public class BaseService : IBaseService
    {
        public IMapper Mapper { get; init; } = null!;
    }
}
