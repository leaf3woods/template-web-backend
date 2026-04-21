using MediatR;

namespace Template.Web.Domain.Events.Handlers
{
    public class UserLoginEventHandler : INotificationHandler<UserLoginEvent>
    {
        public Task Handle(UserLoginEvent notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}