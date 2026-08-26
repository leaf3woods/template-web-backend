using AutoMapper;

namespace Template.Web.Application.Services.Base
{
    public abstract class BaseService : IBaseService
    {
        protected BaseService(IMapper mapper)
        {
            Mapper = mapper;
        }

        protected IMapper Mapper { get; }
    }
}
