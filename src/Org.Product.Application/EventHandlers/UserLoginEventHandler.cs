using MediatR;
using Org.Product.Domain.Events;

namespace Org.Product.Application.EventHandlers;

public class UserLoginEventHandler : INotificationHandler<UserLoginEvent>
{
    public Task Handle(UserLoginEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
