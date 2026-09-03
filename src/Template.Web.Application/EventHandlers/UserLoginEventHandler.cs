using MediatR;
using Template.Web.Domain.Events;

namespace Template.Web.Application.EventHandlers
{
    public class UserLoginEventHandler : INotificationHandler<UserLoginEvent>
    {
        public Task Handle(UserLoginEvent notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
